using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public WishlistController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // GET /Wishlist
    public async Task<IActionResult> Index()
    {
        var userId = _users.GetUserId(User)!;
        var items  = await _db.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();
        return View(items);
    }

    // POST /Wishlist/Toggle  — add if absent, remove if present
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromBody] WishlistToggleRequest req)
    {
        var userId = _users.GetUserId(User)!;
        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Category == req.Category && w.ComponentId == req.ComponentId);

        bool inWishlist;
        if (existing != null)
        {
            _db.WishlistItems.Remove(existing);
            inWishlist = false;
        }
        else
        {
            _db.WishlistItems.Add(new WishlistItem
            {
                UserId        = userId,
                Category      = req.Category,
                ComponentId   = req.ComponentId,
                ComponentName = req.Name[..Math.Min(req.Name.Length, 300)],
                Price         = req.Price,
                ImageUrl      = req.ImageUrl,
            });
            inWishlist = true;
        }
        await _db.SaveChangesAsync();
        return Json(new { inWishlist });
    }

    // GET /Wishlist/Check?category=cpu&id=123  — is item in current user's wishlist?
    [HttpGet]
    public async Task<IActionResult> Check(string category, int id)
    {
        var userId = _users.GetUserId(User)!;
        var exists = await _db.WishlistItems
            .AnyAsync(w => w.UserId == userId && w.Category == category && w.ComponentId == id);
        return Json(new { inWishlist = exists });
    }

    // POST /Wishlist/Remove/{id}
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = _users.GetUserId(User)!;
        var item   = await _db.WishlistItems.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
        if (item == null) return NotFound();
        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }
}

public record WishlistToggleRequest(string Category, int ComponentId, string Name, decimal Price, string? ImageUrl);
