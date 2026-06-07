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

    // GET /Build/Community
    [HttpGet]
    public async Task<IActionResult> Community(int page = 1)
    {
        const int perPage = 12;
        var userId = User.Identity?.IsAuthenticated == true ? _users.GetUserId(User) : null;

        var query  = _db.SavedBuilds.AsNoTracking()
                        .Include(b => b.User)
                        .Where(b => b.IsPublic)
                        .OrderByDescending(b => b.UpvoteCount)
                        .ThenByDescending(b => b.CreatedAt);

        var total  = await query.CountAsync();
        var builds = await query.Skip((page - 1) * perPage).Take(perPage).ToListAsync();

        // Which builds has the current user already upvoted?
        HashSet<int> upvoted = new();
        if (userId != null)
        {
            var ids = builds.Select(b => b.Id).ToList();
            upvoted = (await _db.BuildUpvotes
                        .Where(u => u.UserId == userId && ids.Contains(u.BuildId))
                        .Select(u => u.BuildId)
                        .ToListAsync()).ToHashSet();
        }

        return View(new CommunityViewModel
        {
            Builds      = builds,
            UpvotedIds  = upvoted,
            Page        = page,
            TotalPages  = (int)Math.Ceiling(total / (double)perPage),
        });
    }

    // POST /Build/Publish/{id}  — toggle IsPublic for owner
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var userId = _users.GetUserId(User)!;
        var build = await _db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
        if (build == null) return NotFound();

        build.IsPublic = !build.IsPublic;
        await _db.SaveChangesAsync();
        return Json(new { isPublic = build.IsPublic });
    }

    // POST /Build/Upvote/{id}
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upvote(int id)
    {
        var userId = _users.GetUserId(User)!;
        var build  = await _db.SavedBuilds.FirstOrDefaultAsync(b => b.Id == id && b.IsPublic);
        if (build == null) return NotFound();

        var existing = await _db.BuildUpvotes
                          .FirstOrDefaultAsync(u => u.BuildId == id && u.UserId == userId);
        if (existing != null)
        {
            _db.BuildUpvotes.Remove(existing);
            build.UpvoteCount = Math.Max(0, build.UpvoteCount - 1);
        }
        else
        {
            _db.BuildUpvotes.Add(new BuildUpvote { BuildId = id, UserId = userId });
            build.UpvoteCount++;
        }
        await _db.SaveChangesAsync();
        return Json(new { upvoteCount = build.UpvoteCount, upvoted = existing == null });
    }

    // GET /Build/Compare?a={token1}&b={token2}
    [HttpGet("Build/Compare")]
    public async Task<IActionResult> Compare(string a, string b)
    {
        var buildA = await _db.SavedBuilds.AsNoTracking().Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.ShareToken == a);
        var buildB = await _db.SavedBuilds.AsNoTracking().Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.ShareToken == b);
        if (buildA == null || buildB == null) return NotFound();

        var vm = new CompareViewModel { BuildA = buildA, BuildB = buildB };
        vm.ComponentsA = CompareViewModel.ParseComponents(buildA.BuildJson);
        vm.ComponentsB = CompareViewModel.ParseComponents(buildB.BuildJson);
        return View(vm);
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

public class CommunityViewModel
{
    public List<SavedBuild>  Builds     { get; set; } = new();
    public HashSet<int>      UpvotedIds { get; set; } = new();
    public int Page       { get; set; }
    public int TotalPages { get; set; }
}

public class CompareViewModel
{
    public SavedBuild BuildA { get; set; } = null!;
    public SavedBuild BuildB { get; set; } = null!;
    public Dictionary<string, SharedComponent> ComponentsA { get; set; } = new();
    public Dictionary<string, SharedComponent> ComponentsB { get; set; } = new();
    public decimal TotalA => ComponentsA.Values.Sum(c => c.Price);
    public decimal TotalB => ComponentsB.Values.Sum(c => c.Price);

    public static Dictionary<string, SharedComponent> ParseComponents(string json)
    {
        var result = new Dictionary<string, SharedComponent>();
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (raw == null) return result;
            foreach (var (cat, el) in raw)
            {
                result[cat] = new SharedComponent
                {
                    Id       = el.TryGetProperty("id",       out var idEl)    ? idEl.GetInt32()           : 0,
                    Name     = el.TryGetProperty("name",     out var nameEl)  ? nameEl.GetString() ?? ""  : "",
                    Price    = el.TryGetProperty("price",    out var priceEl) ? (decimal)priceEl.GetDouble() : 0,
                    ImageUrl = el.TryGetProperty("imageUrl", out var imgEl)   ? imgEl.GetString()         : null,
                };
            }
        }
        catch { }
        return result;
    }
}

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
