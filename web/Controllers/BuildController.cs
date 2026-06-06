using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Controllers;

public class BuildController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public BuildController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // POST /Build/Save
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] SaveBuildRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.BuildJson))
            return BadRequest(new { error = "Tên build và dữ liệu là bắt buộc." });

        var userId = _users.GetUserId(User)!;
        var build = new SavedBuild
        {
            UserId    = userId,
            Name      = req.Name.Trim()[..Math.Min(req.Name.Trim().Length, 100)],
            BuildJson = req.BuildJson,
            ShareToken = Guid.NewGuid().ToString("N"),
            CreatedAt  = DateTime.UtcNow,
        };
        _db.SavedBuilds.Add(build);
        await _db.SaveChangesAsync();

        var shareUrl = Url.Action("Share", "Build", new { token = build.ShareToken }, Request.Scheme)!;
        return Json(new { id = build.Id, shareUrl });
    }

    // GET /Build/MyBuilds
    [HttpGet, Authorize]
    public async Task<IActionResult> MyBuilds()
    {
        var userId = _users.GetUserId(User)!;
        var builds = await _db.SavedBuilds
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return View(builds);
    }

    // POST /Build/Delete/{id}
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _users.GetUserId(User)!;
        var build = await _db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (build == null) return NotFound();
        _db.SavedBuilds.Remove(build);
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    // GET /Build/Share/{token}
    [HttpGet("Build/Share/{token}")]
    public async Task<IActionResult> Share(string token)
    {
        var build = await _db.SavedBuilds
            .AsNoTracking()
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.ShareToken == token);
        if (build == null) return NotFound();

        var components = new Dictionary<string, SharedComponent>();
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(build.BuildJson);
            if (raw != null)
            {
                foreach (var (cat, el) in raw)
                {
                    components[cat] = new SharedComponent
                    {
                        Id       = el.TryGetProperty("id",       out var idEl)    ? idEl.GetInt32()         : 0,
                        Name     = el.TryGetProperty("name",     out var nameEl)  ? nameEl.GetString() ?? "" : "",
                        Price    = el.TryGetProperty("price",    out var priceEl) ? (decimal)priceEl.GetDouble() : 0,
                        ImageUrl = el.TryGetProperty("imageUrl", out var imgEl)   ? imgEl.GetString()        : null,
                    };
                }
            }
        }
        catch { /* malformed JSON — show empty build */ }

        return View(new ShareBuildViewModel { Build = build, Components = components });
    }
}

public record SaveBuildRequest(string Name, string BuildJson);

public class ShareBuildViewModel
{
    public SavedBuild Build { get; set; } = null!;
    public Dictionary<string, SharedComponent> Components { get; set; } = new();
    public decimal TotalPrice => Components.Values.Sum(c => c.Price);
}

public class SharedComponent
{
    public int     Id       { get; set; }
    public string  Name     { get; set; } = string.Empty;
    public decimal Price    { get; set; }
    public string? ImageUrl { get; set; }
}
