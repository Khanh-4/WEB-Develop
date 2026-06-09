using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;
using WebBanHang_Bai2.Services;

namespace WebBanHang_Bai2.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly CartService _cart;
    private readonly IOrderRepository _orders;
    private readonly UserManager<ApplicationUser> _userManager;

    public CheckoutController(CartService cart, IOrderRepository orders, UserManager<ApplicationUser> userManager)
    {
        _cart = cart;
        _orders = orders;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var cart = _cart.GetCart();
        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng trống — không thể thanh toán.";
            return RedirectToAction("Index", "Cart");
        }

        var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
        var vm = new CheckoutViewModel
        {
            CustomerName = user?.FullName ?? "",
            Email = user?.Email ?? "",
            Phone = user?.Phone ?? "",
            ShippingAddress = user?.Address ?? "",
        };
        ViewBag.Cart = cart;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Confirm(CheckoutViewModel vm)
    {
        var cart = _cart.GetCart();
        if (cart.Items.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Cart = cart;
            return View("Index", vm);
        }

        var order = new Order
        {
            CustomerName = vm.CustomerName,
            Email = vm.Email,
            Phone = vm.Phone,
            ShippingAddress = vm.ShippingAddress,
            Notes = vm.Notes,
            PaymentMethod = vm.PaymentMethod,
            Subtotal = cart.Subtotal,
            ShippingFee = cart.ShippingFee,
            Total = cart.Total,
            Status = OrderStatus.Pending,
            UserName = User.Identity?.Name,
            Items = cart.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId,
                ProductName = i.Name,
                ImageUrl = i.ImageUrl,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList()
        };
        _orders.Add(order);
        _cart.Clear();

        TempData["Success"] = $"Đặt hàng thành công! Mã đơn: {order.OrderCode}.";
        return RedirectToAction(nameof(Success), new { code = order.OrderCode });
    }

    public IActionResult Success(string code)
    {
        var order = _orders.GetByCode(code);
        if (order is null) return RedirectToAction("Index", "Home");
        return View(order);
    }
}
