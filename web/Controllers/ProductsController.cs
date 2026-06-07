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

    // GET /Products/LiveSearch?q=i5+13400
    [HttpGet]
    public async Task<IActionResult> LiveSearch(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var term = q.Trim().ToLower();

        // Sort in-memory after fetch to avoid EF sharing the LIKE param between
        // Contains (%term%) and StartsWith (term%) which causes PostgreSQL to only
        // match rows where name/manufacturer STARTS with the search term.
        var cpus = (await _db.Cpus.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "cpu" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var mbs = (await _db.Motherboards.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "motherboard" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var rams = (await _db.Memories.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "memory" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var gpus = (await _db.VideoCards.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "gpu" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var storages = (await _db.Storages.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "storage" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var psus = (await _db.PowerSupplies.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "psu" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var cases = (await _db.CaseEnclosures.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "case" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var coolers = (await _db.CpuCoolers.AsNoTracking()
            .Where(x => x.Name.ToLower().Contains(term) || x.Manufacturer.ToLower().Contains(term))
            .OrderBy(x => x.Id).Take(6).Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, Category = "cooler" })
            .ToListAsync())
            .OrderBy(x => x.Name.ToLower().StartsWith(term) ? 0 : 1).Take(3).ToList();

        var all = cpus.Cast<object>().Concat(mbs).Concat(rams).Concat(gpus)
            .Concat(storages).Concat(psus).Concat(cases).Concat(coolers);

        var sorted = all
            .Select(x => (dynamic)x)
            .OrderBy(x => ((string)x.Name).ToLower().StartsWith(term) ? 0 : 1)
            .ThenBy(x => (string)x.Name)
            .Take(6)
            .Select(x => new { x.Id, x.Name, x.Price, x.ImageUrl, x.Category })
            .ToList();

        return Json(sorted);
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
                ["Socket"]      = c.Socket,
                ["Nhân/Luồng"]  = $"{c.CoreCount}C / {c.ThreadCount}T",
                ["Xung cơ sở"]  = $"{c.BaseClock} GHz",
                ["Xung tăng"]   = $"{c.BoostClock} GHz",
                ["TDP"]         = $"{c.TDP} W",
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
                ["Socket"]        = m.SocketCompatibility,
                ["Chipset"]       = string.IsNullOrEmpty(m.Chipset) ? "—" : m.Chipset,
                ["Form Factor"]   = m.FormFactor,
                ["Loại RAM"]      = m.MemoryCompatibility,
                ["Khe RAM"]       = $"{m.MemorySlots}",
                ["RAM tối đa"]    = $"{m.MaxMemoryCapacity} GB",
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
                ["Loại"]       = m.Type,
                ["Dung lượng"] = $"{m.Capacity} GB",
                ["Số thanh"]   = $"{m.Modules}×{m.Capacity / (m.Modules > 0 ? m.Modules : 1)} GB",
                ["Tốc độ"]     = $"{m.Speed} MHz",
                ["Profile"]    = string.IsNullOrEmpty(m.Profile) ? "—" : m.Profile,
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
                ["VRAM"]        = $"{g.VRAM} GB",
                ["Chiều dài"]   = $"{g.Length} mm",
                ["TDP"]         = $"{g.TDP} W",
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
                ["Loại"]        = s.Type,
                ["Dung lượng"]  = FormatStorageCapacity(s.Capacity),
                ["Giao tiếp"]   = s.Interface,
                ["Đọc"]         = s.ReadSpeed > 0 ? $"{s.ReadSpeed} MB/s" : "—",
                ["Ghi"]         = s.WriteSpeed > 0 ? $"{s.WriteSpeed} MB/s" : "—",
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
                ["Công suất"]     = $"{p.Wattage} W",
                ["Hiệu suất"]     = p.Efficiency,
                ["Kiểu dây"]      = p.Modular,
                ["Form Factor"]   = string.IsNullOrEmpty(p.PsuFormFactor) ? "ATX" : p.PsuFormFactor,
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
                ["Loại case"]        = string.IsNullOrEmpty(c.CaseType) ? "—" : c.CaseType,
                ["Hỗ trợ MB"]        = c.FormFactorSupport,
                ["Max GPU"]           = $"{c.MaxVGALength} mm",
                ["Tản nhiệt nước"]   = string.IsNullOrEmpty(c.RadiatorSupport) ? "—" : c.RadiatorSupport,
                ["Màu sắc"]          = c.Color ?? "—",
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
                ["Loại"]        = c.Type,
                ["Max TDP"]     = $"{c.MaxTDP} W",
                ["Chiều cao"]   = $"{c.Height} mm",
                ["Socket"]      = c.SocketCompatibility.Length > 30 ? c.SocketCompatibility[..30] + "…" : c.SocketCompatibility,
            }
        };
    }

    // GET /Products/Compare?category=gpu&ids=1,2,3
    [HttpGet]
    public async Task<IActionResult> Compare(string category, string ids)
    {
        var idList = (ids ?? "")
            .Split(',')
            .Select(x => int.TryParse(x.Trim(), out var n) ? n : 0)
            .Where(x => x > 0).Distinct().Take(3).ToList();

        if (idList.Count < 2) return RedirectToAction("Index");

        var products = new List<ProductDetailViewModel>();
        foreach (var id in idList)
        {
            var vm = category switch
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
            if (vm != null) products.Add(vm);
        }

        if (products.Count < 2) return RedirectToAction("Index");
        return View(products);
    }

    // Returns filter option values for category-specific dropdowns
    [HttpGet]
    public async Task<IActionResult> FilterOptions(string category)
    {
        return category switch
        {
            "motherboard" => Json(new
            {
                sockets     = await _db.Motherboards.AsNoTracking().Select(m => m.SocketCompatibility).Where(s => s != "").Distinct().OrderBy(s => s).ToListAsync(),
                formFactors = await _db.Motherboards.AsNoTracking().Select(m => m.FormFactor).Where(f => f != "").Distinct().OrderBy(f => f).ToListAsync(),
                memTypes    = await _db.Motherboards.AsNoTracking().Select(m => m.MemoryCompatibility).Where(t => t != "").Distinct().OrderBy(t => t).ToListAsync(),
                chipsets    = await _db.Motherboards.AsNoTracking().Select(m => m.Chipset).Where(c => c != "").Distinct().OrderBy(c => c).ToListAsync(),
            }),
            "memory" => Json(new
            {
                types      = await _db.Memories.AsNoTracking().Select(m => m.Type).Where(t => t != "").Distinct().OrderBy(t => t).ToListAsync(),
                capacities = await _db.Memories.AsNoTracking().Select(m => m.Capacity).Where(c => c > 0).Distinct().OrderBy(c => c).ToListAsync(),
                profiles   = await _db.Memories.AsNoTracking().Select(m => m.Profile).Where(p => p != "").Distinct().OrderBy(p => p).ToListAsync(),
            }),
            "gpu" => Json(new
            {
                vrams       = await _db.VideoCards.AsNoTracking().Select(g => g.VRAM).Where(v => v > 0).Distinct().OrderBy(v => v).ToListAsync(),
                generations = (await _db.VideoCards.AsNoTracking().Select(g => g.Name).ToListAsync())
                                .Select(GetGpuGeneration).Where(g => g != "Khác").Distinct().OrderBy(g => g).ToList(),
            }),
            "storage" => Json(new
            {
                types      = await _db.Storages.AsNoTracking().Select(s => s.Type).Where(t => t != "").Distinct().OrderBy(t => t).ToListAsync(),
                interfaces = await _db.Storages.AsNoTracking().Select(s => s.Interface).Where(i => i != "").Distinct().OrderBy(i => i).ToListAsync(),
                capacities = await _db.Storages.AsNoTracking().Select(s => s.Capacity).Where(c => c > 0).Distinct().OrderBy(c => c).ToListAsync(),
            }),
            "psu" => Json(new
            {
                efficiencies = await _db.PowerSupplies.AsNoTracking().Select(p => p.Efficiency).Where(e => e != "").Distinct().OrderBy(e => e).ToListAsync(),
                modulars     = await _db.PowerSupplies.AsNoTracking().Select(p => p.Modular).Where(m => m != "").Distinct().OrderBy(m => m).ToListAsync(),
                formFactors  = await _db.PowerSupplies.AsNoTracking().Select(p => p.PsuFormFactor).Where(f => f != "").Distinct().OrderBy(f => f).ToListAsync(),
            }),
            "case" => Json(new
            {
                caseTypes   = await _db.CaseEnclosures.AsNoTracking().Select(c => c.CaseType).Where(t => t != "").Distinct().OrderBy(t => t).ToListAsync(),
                formFactors = await _db.CaseEnclosures.AsNoTracking().Select(c => c.FormFactorSupport).Where(f => f != "").Distinct().OrderBy(f => f).ToListAsync(),
            }),
            "cooler" => Json(new
            {
                types = await _db.CpuCoolers.AsNoTracking().Select(c => c.Type).Where(t => t != "").Distinct().OrderBy(t => t).ToListAsync(),
            }),
            _ => Json(new { })
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
        string? brands = null,
        // Category-specific filters
        string? socket = null,
        string? formFactor = null,
        string? memType = null,
        int? vram = null,
        string? gpuGen = null,
        string? storageType = null,
        string? storageInterface = null,
        string? efficiency = null,
        string? modular = null,
        int? minWattage = null,
        int? capacity = null,
        string? coolerType = null,
        string? chipset = null,
        string? profile = null,
        string? psuFormFactor = null,
        string? caseType = null)
    {
        var items = await LoadAllAsync(category, search);

        // Price filter
        if (minPrice.HasValue)
            items = items.Where(i => i.Price >= minPrice.Value).ToList();
        if (maxPrice.HasValue && maxPrice.Value > 0)
            items = items.Where(i => i.Price <= maxPrice.Value).ToList();

        // Brand filter
        if (!string.IsNullOrWhiteSpace(brands))
        {
            var brandSet = brands
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (brandSet.Count > 0)
                items = items.Where(i => brandSet.Contains(i.Manufacturer)).ToList();
        }

        // Category-specific filters via FilterData
        if (!string.IsNullOrEmpty(socket))
            items = items.Where(i => i.FilterData.TryGetValue("socket", out var v) &&
                string.Equals(v, socket, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(formFactor))
            items = items.Where(i => i.FilterData.TryGetValue("formFactor", out var v) &&
                v.Contains(formFactor, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(memType))
            items = items.Where(i => i.FilterData.TryGetValue("memType", out var v) &&
                string.Equals(v, memType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (vram.HasValue)
            items = items.Where(i => i.FilterData.TryGetValue("vram", out var v) &&
                int.TryParse(v, out var n) && n == vram.Value).ToList();

        if (!string.IsNullOrEmpty(gpuGen))
            items = items.Where(i => i.FilterData.TryGetValue("generation", out var v) &&
                string.Equals(v, gpuGen, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(storageType))
            items = items.Where(i => i.FilterData.TryGetValue("storageType", out var v) &&
                string.Equals(v, storageType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(storageInterface))
            items = items.Where(i => i.FilterData.TryGetValue("interface", out var v) &&
                v.Contains(storageInterface, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(efficiency))
            items = items.Where(i => i.FilterData.TryGetValue("efficiency", out var v) &&
                v.Contains(efficiency, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(modular))
            items = items.Where(i => i.FilterData.TryGetValue("modular", out var v) &&
                string.Equals(v, modular, StringComparison.OrdinalIgnoreCase)).ToList();

        if (minWattage.HasValue)
            items = items.Where(i => i.FilterData.TryGetValue("wattage", out var v) &&
                int.TryParse(v, out var n) && n >= minWattage.Value).ToList();

        if (capacity.HasValue)
        {
            if (category == "memory")
                items = items.Where(i => i.FilterData.TryGetValue("capacity", out var v) &&
                    v == capacity.Value.ToString()).ToList();
            else if (category == "storage")
                items = items.Where(i => i.FilterData.TryGetValue("capacity", out var v) &&
                    int.TryParse(v, out var n) && n >= capacity.Value).ToList();
        }

        if (!string.IsNullOrEmpty(coolerType))
            items = items.Where(i => i.FilterData.TryGetValue("coolerType", out var v) &&
                string.Equals(v, coolerType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(chipset))
            items = items.Where(i => i.FilterData.TryGetValue("chipset", out var v) &&
                string.Equals(v, chipset, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(profile))
            items = items.Where(i => i.FilterData.TryGetValue("profile", out var v) &&
                string.Equals(v, profile, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(psuFormFactor))
            items = items.Where(i => i.FilterData.TryGetValue("psuFormFactor", out var v) &&
                string.Equals(v, psuFormFactor, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!string.IsNullOrEmpty(caseType))
            items = items.Where(i => i.FilterData.TryGetValue("caseType", out var v) &&
                string.Equals(v, caseType, StringComparison.OrdinalIgnoreCase)).ToList();

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

        if (all || category == "cpu")
            brands.AddRange(await _db.Cpus.AsNoTracking()
                .Where(c => !c.Name.ToLower().StartsWith("pc "))
                .Select(c => c.Manufacturer).Distinct().ToListAsync());

        if (all || category == "prebuilt")
            brands.AddRange(await _db.Cpus.AsNoTracking()
                .Where(c => c.Name.ToLower().StartsWith("pc "))
                .Select(c => c.Manufacturer).Distinct().ToListAsync());

        if (all || category == "motherboard")
            brands.AddRange(await _db.Motherboards.AsNoTracking().Select(m => m.Manufacturer).Distinct().ToListAsync());

        if (all || category == "memory")
            brands.AddRange(await _db.Memories.AsNoTracking().Select(m => m.Manufacturer).Distinct().ToListAsync());

        if (all || category == "gpu")
            brands.AddRange(await _db.VideoCards.AsNoTracking().Select(g => g.Manufacturer).Distinct().ToListAsync());

        if (all || category == "storage")
            brands.AddRange(await _db.Storages.AsNoTracking().Select(s => s.Manufacturer).Distinct().ToListAsync());

        if (all || category == "psu")
            brands.AddRange(await _db.PowerSupplies.AsNoTracking().Select(p => p.Manufacturer).Distinct().ToListAsync());

        if (all || category == "case")
            brands.AddRange(await _db.CaseEnclosures.AsNoTracking().Select(c => c.Manufacturer).Distinct().ToListAsync());

        if (all || category == "cooler")
            brands.AddRange(await _db.CpuCoolers.AsNoTracking().Select(c => c.Manufacturer).Distinct().ToListAsync());

        // MB and RAM don't have AMD/Intel as board/module makers — exclude chip-vendor names
        var excludeForCats = new[] { "motherboard", "memory" };
        var result = brands
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b)
            .ToList();

        if (excludeForCats.Contains(category))
            result = result
                .Where(b => !b.Equals("AMD", StringComparison.OrdinalIgnoreCase) &&
                             !b.Equals("Intel", StringComparison.OrdinalIgnoreCase))
                .ToList();

        return Json(result);
    }

    private async Task<List<ProductListItem>> LoadAllAsync(string category, string? search)
    {
        var result = new List<ProductListItem>();
        bool all = category == "all";
        bool isCpu = category == "cpu";
        bool isPrebuilt = category == "prebuilt";
        string? q = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // CPU + Prebuilt are both in the cpu table
        if (all || isCpu || isPrebuilt)
        {
            var query = _db.Cpus.AsNoTracking()
                .Where(c => q == null || c.Name.ToLower().Contains(q) || c.Manufacturer.ToLower().Contains(q));

            if (isCpu)    query = query.Where(c => !c.Name.ToLower().StartsWith("pc "));
            if (isPrebuilt) query = query.Where(c => c.Name.ToLower().StartsWith("pc "));

            var rows = await query.ToListAsync();
            result.AddRange(rows.Select(c =>
            {
                bool prebuilt = c.Name.StartsWith("PC ", StringComparison.OrdinalIgnoreCase);
                return new ProductListItem
                {
                    Id = c.Id, Category = "cpu", Name = c.Name,
                    Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                    IsPrebuilt = prebuilt,
                    PpScore = c.Price > 0 ? Math.Round((double)c.ApproximatePerformance / (double)c.Price * 1_000_000, 2) : 0,
                    Specs = prebuilt
                        ? new()
                        : new() { ["Socket"] = c.Socket, ["Nhân/Luồng"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["TDP"] = $"{c.TDP}W" },
                    FilterData = new() { ["socket"] = c.Socket }
                };
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
                Specs = new() { ["Socket"] = m.SocketCompatibility, ["Chuẩn"] = m.FormFactor, ["RAM"] = m.MemoryCompatibility },
                FilterData = new() { ["socket"] = m.SocketCompatibility, ["formFactor"] = m.FormFactor, ["memType"] = m.MemoryCompatibility, ["chipset"] = m.Chipset }
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
                Specs = new() { ["Loại"] = m.Type, ["Dung lượng"] = $"{m.Capacity}GB", ["Tốc độ"] = $"{m.Speed}MHz" },
                FilterData = new() { ["memType"] = m.Type, ["capacity"] = m.Capacity.ToString(), ["profile"] = m.Profile }
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
                Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["Chiều dài"] = $"{g.Length}mm", ["TDP"] = $"{g.TDP}W" },
                FilterData = new() { ["vram"] = g.VRAM.ToString(), ["generation"] = GetGpuGeneration(g.Name) }
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
                Specs = new() { ["Loại"] = s.Type, ["Dung lượng"] = FormatStorageCapacity(s.Capacity), ["Giao tiếp"] = s.Interface },
                FilterData = new() { ["storageType"] = s.Type, ["interface"] = s.Interface, ["capacity"] = s.Capacity.ToString() }
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
                Specs = new() { ["Công suất"] = $"{p.Wattage}W", ["Hiệu suất"] = p.Efficiency, ["Kiểu dây"] = p.Modular },
                FilterData = new() { ["wattage"] = p.Wattage.ToString(), ["efficiency"] = p.Efficiency, ["modular"] = p.Modular, ["psuFormFactor"] = p.PsuFormFactor }
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
                Specs = new() { ["Hỗ trợ MB"] = c.FormFactorSupport, ["Max GPU"] = $"{c.MaxVGALength}mm", ["Màu sắc"] = c.Color ?? "—" },
                FilterData = new() { ["formFactor"] = c.FormFactorSupport, ["caseType"] = c.CaseType, ["radiatorSupport"] = c.RadiatorSupport }
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
                Specs = new() { ["Loại"] = c.Type, ["Max TDP"] = $"{c.MaxTDP}W", ["Socket"] = c.SocketCompatibility.Length > 30 ? c.SocketCompatibility[..30] + "…" : c.SocketCompatibility },
                FilterData = new() { ["coolerType"] = c.Type }
            }));
        }

        return result;
    }

    private static List<ProductListItem> ApplySort(List<ProductListItem> items, string sort) => sort switch
    {
        "price-asc"  => items.OrderBy(x => x.Price).ToList(),
        "price-desc" => items.OrderByDescending(x => x.Price).ToList(),
        "name"       => Interleave(items),
        _            => items.OrderByDescending(x => x.PpScore).ToList(),
    };

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

    private static string GetGpuGeneration(string name)
    {
        var n = name.ToUpperInvariant();
        if (n.Contains("RTX 50") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b50[678]0\b")) return "GeForce RTX 50";
        if (n.Contains("RTX 40") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b40[678]0\b")) return "GeForce RTX 40";
        if (n.Contains("RTX 30") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b30[678]0\b")) return "GeForce RTX 30";
        if (n.Contains("RTX 20") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b20[678]0\b")) return "GeForce RTX 20";
        if (n.Contains("GTX 16") || System.Text.RegularExpressions.Regex.IsMatch(n, @"\b16[56]0\b"))  return "GeForce GTX 16";
        if (n.Contains("RX 9") || n.Contains("RX9"))  return "Radeon RX 9000";
        if (n.Contains("RX 7") || n.Contains("RX7"))  return "Radeon RX 7000";
        if (n.Contains("RX 6") || n.Contains("RX6"))  return "Radeon RX 6000";
        if (n.Contains("ARC"))  return "Intel Arc";
        return "Khác";
    }

    private static string FormatStorageCapacity(int gb) => gb switch
    {
        >= 1024 when gb % 1024 == 0 => $"{gb / 1024} TB",
        >= 1024 => $"{gb / 1024.0:0.#} TB",
        _ => $"{gb} GB"
    };
}
