using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CartController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await GetOrCreateCartAsync();
        return View(BuildCartViewModel(cart));
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Category) || req.ComponentId <= 0)
            return BadRequest(new { error = "Invalid request" });

        var cart = await GetOrCreateCartAsync();

        var existing = cart.Items.FirstOrDefault(i =>
            i.Category == req.Category && i.ComponentId == req.ComponentId);

        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                Category = req.Category,
                ComponentId = req.ComponentId,
                ComponentName = req.Name,
                Price = req.Price,
                Quantity = 1,
                ImageUrl = req.ImageUrl,
            });
        }

        await _db.SaveChangesAsync();

        int count = cart.Items.Sum(i => i.Quantity);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] RemoveFromCartRequest req)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == req.CartItemId);
        if (item == null) return NotFound();

        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync();

        int count = cart.Items.Where(i => i.Id != req.CartItemId).Sum(i => i.Quantity);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateQty([FromBody] UpdateQtyRequest req)
    {
        if (req.Quantity < 1) return BadRequest();

        var cart = await GetOrCreateCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == req.CartItemId);
        if (item == null) return NotFound();

        item.Quantity = req.Quantity;
        await _db.SaveChangesAsync();

        int count = cart.Items.Sum(i => i.Id == req.CartItemId ? req.Quantity : i.Quantity);
        decimal total = cart.Items.Sum(i => i.Price * (i.Id == req.CartItemId ? req.Quantity : i.Quantity));
        return Ok(new { count, total });
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var cart = await GetOrCreateCartAsync();
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();
        return Ok(new { count = 0 });
    }

    // AJAX endpoint for navbar badge
    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var userId = _users.GetUserId(User)!;
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);
        int count = cart?.Items.Sum(i => i.Quantity) ?? 0;
        return Ok(new { count });
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Cart> GetOrCreateCartAsync()
    {
        var userId = _users.GetUserId(User)!;
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart { UserId = userId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }

        return cart;
    }

    private static CartViewModel BuildCartViewModel(Cart cart) => new()
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
}

public record AddToCartRequest(string Category, int ComponentId, string Name, decimal Price, string? ImageUrl);
public record RemoveFromCartRequest(int CartItemId);
public record UpdateQtyRequest(int CartItemId, int Quantity);
