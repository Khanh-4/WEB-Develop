using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public OrdersController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // GET /Checkout
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = _users.GetUserId(User)!;
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var vm = new CheckoutViewModel
        {
            RecipientName = User.Identity?.Name?.Split('@')[0] ?? string.Empty,
            Cart = BuildCartVm(cart)
        };

        return View(vm);
    }

    // POST /Checkout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        var userId = _users.GetUserId(User)!;
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        vm.Cart = BuildCartVm(cart);

        if (!ModelState.IsValid)
            return View(vm);

        // Stock check — verify each item has enough stock before placing order
        foreach (var item in cart.Items)
        {
            int stock = item.Category switch
            {
                "cpu"         => (await _db.Cpus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "motherboard" => (await _db.Motherboards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "memory"      => (await _db.Memories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "gpu"         => (await _db.VideoCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "storage"     => (await _db.Storages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "psu"         => (await _db.PowerSupplies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "case"        => (await _db.CaseEnclosures.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                "cooler"      => (await _db.CpuCoolers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.ComponentId))?.Stock ?? 0,
                _             => 999
            };
            if (stock < item.Quantity)
                ModelState.AddModelError("", $"Sản phẩm \"{item.ComponentName}\" hiện không đủ hàng (còn {stock}).");
        }

        if (!ModelState.IsValid)
            return View(vm);

        var subtotal = cart.Items.Sum(i => i.Price * i.Quantity);
        var discount = ApplyCoupon(vm.CouponCode, subtotal);

        var order = new Order
        {
            UserId = userId,
            TotalAmount = subtotal - discount,
            DiscountAmount = discount,
            Status = OrderStatus.Pending,
            PaymentMethod = vm.PaymentMethod,
            RecipientName = vm.RecipientName,
            Phone = vm.Phone,
            ShippingAddress = vm.ShippingAddress,
            Note = vm.Note,
            Details = cart.Items.Select(i => new OrderDetail
            {
                Category = i.Category,
                ComponentId = i.ComponentId,
                ComponentName = i.ComponentName,
                Price = i.Price,
                Quantity = i.Quantity,
                ImageUrl = i.ImageUrl,
            }).ToList()
        };

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    // GET /Orders/CheckCoupon?code=X&subtotal=Y
    [HttpGet]
    public IActionResult CheckCoupon(string? code, decimal subtotal)
    {
        var discount = ApplyCoupon(code, subtotal);
        bool valid = discount > 0;
        return Json(new { valid, discount, total = subtotal - discount });
    }

    // GET /Orders/Confirmation/5
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var userId = _users.GetUserId(User)!;
        var order = await _db.Orders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        return View(BuildDetailVm(order));
    }

    // GET /Orders
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User)!;
        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Details)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new OrderListViewModel
        {
            Orders = orders.Select(o => new OrderSummaryViewModel
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details.Sum(d => d.Quantity),
            }).ToList()
        };

        return View(vm);
    }

    // GET /Orders/Detail/5
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var userId = _users.GetUserId(User)!;
        var order = await _db.Orders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();

        return View(BuildDetailVm(order));
    }

    // POST /Orders/Cancel/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = _users.GetUserId(User)!;
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (order == null) return NotFound();
        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng ở trạng thái Chờ xử lý.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã hủy đơn hàng thành công.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static decimal ApplyCoupon(string? code, decimal subtotal) => code?.ToUpper() switch
    {
        "TECHSPECS10" => Math.Round(subtotal * 0.10m),
        _             => 0m,
    };

    private static CartViewModel BuildCartVm(Cart cart) => new()
    {
        Items = cart.Items.Select(i => new CartItemViewModel
        {
            CartItemId = i.Id,
            Category = i.Category,
            ComponentId = i.ComponentId,
            ComponentName = i.ComponentName,
            Price = i.Price,
            Quantity = i.Quantity,
            ImageUrl = i.ImageUrl,
        }).ToList()
    };

    private static OrderDetailViewModel BuildDetailVm(Order o) => new()
    {
        Id = o.Id,
        Status = o.Status,
        PaymentMethod = o.PaymentMethod,
        TotalAmount = o.TotalAmount,
        DiscountAmount = o.DiscountAmount,
        RecipientName = o.RecipientName,
        Phone = o.Phone,
        ShippingAddress = o.ShippingAddress,
        Note = o.Note,
        CreatedAt = o.CreatedAt,
        Items = o.Details.Select(d => new OrderDetailItemViewModel
        {
            Category = d.Category,
            ComponentId = d.ComponentId,
            ComponentName = d.ComponentName,
            Price = d.Price,
            Quantity = d.Quantity,
            ImageUrl = d.ImageUrl,
        }).ToList()
    };
}
