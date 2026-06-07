using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _db;
    private const int PageSize = 24;

    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(
        string category = "all",
        string? search = null,
        string sort = "name",
        int page = 1)
    {
        var items = await LoadAllAsync(category, search);
        items = ApplySort(items, sort);

        int total = items.Count;
        var paged = items.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        return View(new ProductsIndexViewModel
        {
            Products = paged,
            SelectedCategory = category,
            SearchQuery = search,
            SortBy = sort,
            Page = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize),
            TotalCount = total,
        });
    }

    // GET /Products/PriceHistory?category=cpu&name={productName}
    [HttpGet]
    public async Task<IActionResult> PriceHistory(string category, string name)
    {
        var rows = await _db.PriceHistories
            .AsNoTracking()
            .Where(p => p.Category == category && p.ProductName == name)
            .OrderBy(p => p.RecordedAt)
            .Select(p => new { date = p.RecordedAt.ToString("yyyy-MM-dd"), price = p.Price })
            .ToListAsync();
        return Json(rows);
    }

    [HttpGet("Products/Detail/{category}/{id:int}")]
    public async Task<IActionResult> Detail(string category, int id)
    {
        ProductDetailViewModel? vm = category switch
        {
            "cpu"         => await BuildCpuDetailAsync(id),
            "motherboard" => await BuildMbDetailAsync(id),
            "memory"      => await BuildMemoryDetailAsync(id),
            "gpu"         => await BuildGpuDetailAsync(id),
            "storage"     => await BuildStorageDetailAsync(id),
            "psu"         => await BuildPsuDetailAsync(id),
            "case"        => await BuildCaseDetailAsync(id),
            "cooler"      => await BuildCoolerDetailAsync(id),
            _             => null,
        };
        if (vm == null) return NotFound();
        return View(vm);
    }

    private async Task<ProductDetailViewModel?> BuildCpuDetailAsync(int id)
    {
        var c = await _db.Cpus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return null;
        return new ProductDetailViewModel
        {
            Id = c.Id, Category = "cpu", Name = c.Name, Manufacturer = c.Manufacturer,
            Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
            Specs = new()
            {
                ["Socket"]     = c.Socket,
                ["Cores"]      = $"{c.CoreCount}C / {c.ThreadCount}T",
                ["Base Clock"] = $"{c.BaseClock} GHz",
                ["Boost Clock"]= $"{c.BoostClock} GHz",
                ["TDP"]        = $"{c.TDP} W",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildMbDetailAsync(int id)
    {
        var m = await _db.Motherboards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return null;
        return new ProductDetailViewModel
        {
            Id = m.Id, Category = "motherboard", Name = m.Name, Manufacturer = m.Manufacturer,
            Price = m.Price, ImageUrl = m.ImageUrl, Stock = m.Stock,
            Specs = new()
            {
                ["Socket"]       = m.SocketCompatibility,
                ["Form Factor"]  = m.FormFactor,
                ["Memory Type"]  = m.MemoryCompatibility,
                ["Memory Slots"] = $"{m.MemorySlots}",
                ["Max Memory"]   = $"{m.MaxMemoryCapacity} GB",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildMemoryDetailAsync(int id)
    {
        var m = await _db.Memories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return null;
        return new ProductDetailViewModel
        {
            Id = m.Id, Category = "memory", Name = m.Name, Manufacturer = m.Manufacturer,
            Price = m.Price, ImageUrl = m.ImageUrl, Stock = m.Stock,
            Specs = new()
            {
                ["Type"]     = m.Type,
                ["Capacity"] = $"{m.Capacity} GB",
                ["Modules"]  = $"{m.Modules}x{m.Capacity / (m.Modules > 0 ? m.Modules : 1)} GB",
                ["Speed"]    = $"{m.Speed} MHz",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildGpuDetailAsync(int id)
    {
        var g = await _db.VideoCards.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (g == null) return null;
        return new ProductDetailViewModel
        {
            Id = g.Id, Category = "gpu", Name = g.Name, Manufacturer = g.Manufacturer,
            Price = g.Price, ImageUrl = g.ImageUrl, Stock = g.Stock,
            Specs = new()
            {
                ["VRAM"]   = $"{g.VRAM} GB",
                ["Length"] = $"{g.Length} mm",
                ["TDP"]    = $"{g.TDP} W",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildStorageDetailAsync(int id)
    {
        var s = await _db.Storages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return null;
        return new ProductDetailViewModel
        {
            Id = s.Id, Category = "storage", Name = s.Name, Manufacturer = s.Manufacturer,
            Price = s.Price, ImageUrl = s.ImageUrl, Stock = s.Stock,
            Specs = new()
            {
                ["Type"]        = s.Type,
                ["Capacity"]    = $"{s.Capacity} GB",
                ["Interface"]   = s.Interface,
                ["Read Speed"]  = s.ReadSpeed > 0 ? $"{s.ReadSpeed} MB/s" : "—",
                ["Write Speed"] = s.WriteSpeed > 0 ? $"{s.WriteSpeed} MB/s" : "—",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildPsuDetailAsync(int id)
    {
        var p = await _db.PowerSupplies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return null;
        return new ProductDetailViewModel
        {
            Id = p.Id, Category = "psu", Name = p.Name, Manufacturer = p.Manufacturer,
            Price = p.Price, ImageUrl = p.ImageUrl, Stock = p.Stock,
            Specs = new()
            {
                ["Wattage"]    = $"{p.Wattage} W",
                ["Efficiency"] = p.Efficiency,
                ["Modular"]    = p.Modular,
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildCaseDetailAsync(int id)
    {
        var c = await _db.CaseEnclosures.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return null;
        return new ProductDetailViewModel
        {
            Id = c.Id, Category = "case", Name = c.Name, Manufacturer = c.Manufacturer,
            Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
            Specs = new()
            {
                ["Form Factor Support"] = c.FormFactorSupport,
                ["Max GPU Length"]      = $"{c.MaxVGALength} mm",
                ["Color"]               = c.Color ?? "—",
            }
        };
    }

    private async Task<ProductDetailViewModel?> BuildCoolerDetailAsync(int id)
    {
        var c = await _db.CpuCoolers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return null;
        return new ProductDetailViewModel
        {
            Id = c.Id, Category = "cooler", Name = c.Name, Manufacturer = c.Manufacturer,
            Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
            Specs = new()
            {
                ["Type"]               = c.Type,
                ["Max TDP"]            = $"{c.MaxTDP} W",
                ["Height"]             = $"{c.Height} mm",
                ["Socket Support"]     = c.SocketCompatibility,
            }
        };
    }

    // AJAX endpoint for filter changes
    [HttpGet]
    public async Task<IActionResult> Filter(
        string category = "all",
        string? search = null,
        string sort = "pp",
        int page = 1,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? brands = null)
    {
        var items = await LoadAllAsync(category, search);

        if (minPrice.HasValue)
            items = items.Where(i => i.Price >= minPrice.Value).ToList();
        if (maxPrice.HasValue && maxPrice.Value > 0)
            items = items.Where(i => i.Price <= maxPrice.Value).ToList();
        if (!string.IsNullOrWhiteSpace(brands))
        {
            var brandSet = brands
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (brandSet.Count > 0)
                items = items.Where(i => brandSet.Contains(i.Manufacturer)).ToList();
        }

        items = ApplySort(items, sort);

        int total = items.Count;
        var paged = items.Skip((page - 1) * PageSize).Take(PageSize).ToList();

        return Json(new
        {
            products = paged,
            totalCount = total,
            totalPages = (int)Math.Ceiling(total / (double)PageSize),
            page,
        });
    }

    // Returns distinct brands for a given category (for brand filter checkboxes)
    [HttpGet]
    public async Task<IActionResult> Brands(string category = "all")
    {
        bool all = category == "all";
        var brands = new List<string>();

        if (all || category == "cpu")         brands.AddRange(await _db.Cpus.AsNoTracking().Select(c => c.Manufacturer).Distinct().ToListAsync());
        if (all || category == "motherboard") brands.AddRange(await _db.Motherboards.AsNoTracking().Select(m => m.Manufacturer).Distinct().ToListAsync());
        if (all || category == "memory")      brands.AddRange(await _db.Memories.AsNoTracking().Select(m => m.Manufacturer).Distinct().ToListAsync());
        if (all || category == "gpu")         brands.AddRange(await _db.VideoCards.AsNoTracking().Select(g => g.Manufacturer).Distinct().ToListAsync());
        if (all || category == "storage")     brands.AddRange(await _db.Storages.AsNoTracking().Select(s => s.Manufacturer).Distinct().ToListAsync());
        if (all || category == "psu")         brands.AddRange(await _db.PowerSupplies.AsNoTracking().Select(p => p.Manufacturer).Distinct().ToListAsync());
        if (all || category == "case")        brands.AddRange(await _db.CaseEnclosures.AsNoTracking().Select(c => c.Manufacturer).Distinct().ToListAsync());
        if (all || category == "cooler")      brands.AddRange(await _db.CpuCoolers.AsNoTracking().Select(c => c.Manufacturer).Distinct().ToListAsync());

        return Json(brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(b => b).ToList());
    }

    private async Task<List<ProductListItem>> LoadAllAsync(string category, string? search)
    {
        var result = new List<ProductListItem>();
        bool all = category == "all";
        string? q = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        if (all || category == "cpu")
        {
            var rows = await _db.Cpus.AsNoTracking()
                .Where(c => q == null || c.Name.ToLower().Contains(q) || c.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cpu", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                PpScore = c.Price > 0 ? Math.Round((double)c.ApproximatePerformance / (double)c.Price * 1_000_000, 2) : 0,
                Specs = new() { ["Socket"] = c.Socket, ["Cores"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["TDP"] = $"{c.TDP}W" }
            }));
        }
        if (all || category == "motherboard")
        {
            var rows = await _db.Motherboards.AsNoTracking()
                .Where(m => q == null || m.Name.ToLower().Contains(q) || m.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(m => new ProductListItem
            {
                Id = m.Id, Category = "motherboard", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                PpScore = m.Price > 0 ? Math.Round(10_000_000_000d / (double)m.Price, 2) : 0,
                Specs = new() { ["Socket"] = m.SocketCompatibility, ["Form"] = m.FormFactor, ["RAM"] = m.MemoryCompatibility }
            }));
        }
        if (all || category == "memory")
        {
            var rows = await _db.Memories.AsNoTracking()
                .Where(m => q == null || m.Name.ToLower().Contains(q) || m.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(m => new ProductListItem
            {
                Id = m.Id, Category = "memory", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                PpScore = m.Price > 0 ? Math.Round((double)(m.Capacity * m.Speed) / (double)m.Price * 1000, 2) : 0,
                Specs = new() { ["Type"] = m.Type, ["Capacity"] = $"{m.Capacity}GB", ["Speed"] = $"{m.Speed}MHz" }
            }));
        }
        if (all || category == "gpu")
        {
            var rows = await _db.VideoCards.AsNoTracking()
                .Where(g => q == null || g.Name.ToLower().Contains(q) || g.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(g => new ProductListItem
            {
                Id = g.Id, Category = "gpu", Name = g.Name,
                Manufacturer = g.Manufacturer, Price = g.Price, ImageUrl = g.ImageUrl,
                PpScore = g.Price > 0 ? Math.Round((double)g.ApproximatePerformance / (double)g.Price * 1_000_000, 2) : 0,
                Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["Length"] = $"{g.Length}mm", ["TDP"] = $"{g.TDP}W" }
            }));
        }
        if (all || category == "storage")
        {
            var rows = await _db.Storages.AsNoTracking()
                .Where(s => q == null || s.Name.ToLower().Contains(q) || s.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(s => new ProductListItem
            {
                Id = s.Id, Category = "storage", Name = s.Name,
                Manufacturer = s.Manufacturer, Price = s.Price, ImageUrl = s.ImageUrl,
                PpScore = s.Price > 0 ? Math.Round((double)s.Capacity / (double)s.Price * 1_000_000, 2) : 0,
                Specs = new() { ["Type"] = s.Type, ["Capacity"] = $"{s.Capacity}GB", ["Interface"] = s.Interface }
            }));
        }
        if (all || category == "psu")
        {
            var rows = await _db.PowerSupplies.AsNoTracking()
                .Where(p => q == null || p.Name.ToLower().Contains(q) || p.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(p => new ProductListItem
            {
                Id = p.Id, Category = "psu", Name = p.Name,
                Manufacturer = p.Manufacturer, Price = p.Price, ImageUrl = p.ImageUrl,
                PpScore = p.Price > 0 ? Math.Round((double)p.Wattage / (double)p.Price * 100_000, 2) : 0,
                Specs = new() { ["Wattage"] = $"{p.Wattage}W", ["Efficiency"] = p.Efficiency, ["Modular"] = p.Modular }
            }));
        }
        if (all || category == "case")
        {
            var rows = await _db.CaseEnclosures.AsNoTracking()
                .Where(c => q == null || c.Name.ToLower().Contains(q) || c.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(c => new ProductListItem
            {
                Id = c.Id, Category = "case", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                PpScore = c.Price > 0 ? Math.Round(10_000_000_000d / (double)c.Price, 2) : 0,
                Specs = new() { ["Form Factor"] = c.FormFactorSupport, ["Max GPU"] = $"{c.MaxVGALength}mm", ["Color"] = c.Color ?? "—" }
            }));
        }
        if (all || category == "cooler")
        {
            var rows = await _db.CpuCoolers.AsNoTracking()
                .Where(c => q == null || c.Name.ToLower().Contains(q) || c.Manufacturer.ToLower().Contains(q))
                .ToListAsync();
            result.AddRange(rows.Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cooler", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                PpScore = c.Price > 0 ? Math.Round((double)c.MaxTDP / (double)c.Price * 1_000_000, 2) : 0,
                Specs = new() { ["Type"] = c.Type, ["Max TDP"] = $"{c.MaxTDP}W", ["Socket"] = c.SocketCompatibility.Length > 30 ? c.SocketCompatibility[..30] + "…" : c.SocketCompatibility }
            }));
        }

        return result;
    }

    private static List<ProductListItem> ApplySort(List<ProductListItem> items, string sort) => sort switch
    {
        "price-asc"  => items.OrderBy(x => x.Price).ToList(),
        "price-desc" => items.OrderByDescending(x => x.Price).ToList(),
        "name"       => Interleave(items),   // "all" view: round-robin across categories
        _            => items.OrderByDescending(x => x.PpScore).ToList(),
    };

    /// Round-robin across categories so "all" view shows variety on every page.
    private static List<ProductListItem> Interleave(List<ProductListItem> items)
    {
        var groups = items
            .GroupBy(x => x.Category)
            .Select(g => g.OrderByDescending(x => x.PpScore).ToList())
            .ToList();

        var result = new List<ProductListItem>();
        int maxLen = groups.Max(g => g.Count);
        for (int i = 0; i < maxLen; i++)
            foreach (var g in groups)
                if (i < g.Count) result.Add(g[i]);

        return result;
    }
}
