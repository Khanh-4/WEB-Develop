using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // GET /Admin
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    // ── Dashboard ────────────────────────────────────────────────

    public async Task<IActionResult> Dashboard()
    {
        var orders = await _db.Orders.Include(o => o.Details).ToListAsync();
        var vm = new AdminDashboardViewModel
        {
            TotalUsers    = await _db.Users.CountAsync(),
            TotalOrders   = orders.Count,
            TotalRevenue  = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.TotalAmount),
            PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending),
            TotalProducts = await _db.Cpus.CountAsync() + await _db.Motherboards.CountAsync()
                          + await _db.Memories.CountAsync() + await _db.VideoCards.CountAsync()
                          + await _db.PowerSupplies.CountAsync() + await _db.CaseEnclosures.CountAsync()
                          + await _db.Storages.CountAsync() + await _db.CpuCoolers.CountAsync(),
            RecentOrders = orders
                .OrderByDescending(o => o.CreatedAt).Take(5)
                .Select(o => new OrderSummaryViewModel
                {
                    Id = o.Id, TotalAmount = o.TotalAmount, Status = o.Status,
                    CreatedAt = o.CreatedAt, ItemCount = o.Details.Sum(d => d.Quantity)
                }).ToList(),
            ProductCountByCategory = new()
            {
                ["CPU"]         = await _db.Cpus.CountAsync(),
                ["Motherboard"] = await _db.Motherboards.CountAsync(),
                ["RAM"]         = await _db.Memories.CountAsync(),
                ["GPU"]         = await _db.VideoCards.CountAsync(),
                ["PSU"]         = await _db.PowerSupplies.CountAsync(),
                ["Case"]        = await _db.CaseEnclosures.CountAsync(),
                ["Storage"]     = await _db.Storages.CountAsync(),
                ["Cooler"]      = await _db.CpuCoolers.CountAsync(),
            }
        };
        return View(vm);
    }

    // ── Products ─────────────────────────────────────────────────

    public async Task<IActionResult> Products(string category = "cpu", int page = 1)
    {
        const int pageSize = 20;
        var (items, total) = await LoadProductsAsync(category, page, pageSize);
        ViewBag.Category  = category;
        ViewBag.Page      = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.Total     = total;
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(string category, int id = 0)
    {
        var vm = id == 0
            ? new AdminProductEditViewModel { Category = category }
            : await LoadProductVmAsync(category, id);

        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(AdminProductEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        if (vm.Id == 0)
            await CreateProductAsync(vm);
        else
            await UpdateProductAsync(vm);

        await _db.SaveChangesAsync();
        TempData["Success"] = vm.Id == 0 ? "Đã thêm sản phẩm" : "Đã cập nhật sản phẩm";
        return RedirectToAction(nameof(Products), new { category = vm.Category });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(string category, int id)
    {
        await DeleteProductAsync(category, id);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá sản phẩm";
        return RedirectToAction(nameof(Products), new { category });
    }

    // ── Orders ───────────────────────────────────────────────────

    public async Task<IActionResult> Orders(string? status = null)
    {
        var q = _db.Orders.Include(o => o.Details)
            .Join(_db.Users, o => o.UserId, u => u.Id, (o, u) => new { o, u })
            .AsQueryable();

        if (status != null && Enum.TryParse<OrderStatus>(status, out var s))
            q = q.Where(x => x.o.Status == s);

        var rows = await q.OrderByDescending(x => x.o.CreatedAt).ToListAsync();
        var vm = new AdminOrdersViewModel
        {
            StatusFilter = status,
            Orders = rows.Select(x => new AdminOrderRowViewModel
            {
                Id = x.o.Id,
                UserEmail = x.u.Email ?? x.u.UserName ?? "—",
                RecipientName = x.o.RecipientName,
                Phone = x.o.Phone,
                ShippingAddress = x.o.ShippingAddress,
                TotalAmount = x.o.TotalAmount,
                Status = x.o.Status,
                CreatedAt = x.o.CreatedAt,
                ItemCount = x.o.Details.Sum(d => d.Quantity),
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _db.SaveChangesAsync();
        return Ok(new { status = status.ToString() });
    }

    // ── Scraper ──────────────────────────────────────────────────

    public IActionResult Scraper() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult RunScraper(string categories = "")
    {
        var scraperDir  = Path.Combine(Directory.GetCurrentDirectory(), "..", "scraper");
        var venvPython  = Path.Combine(scraperDir, "venv", "bin", "python");
        var pythonBin   = System.IO.File.Exists(venvPython) ? venvPython : "python3";
        var args = string.IsNullOrWhiteSpace(categories) ? "main.py" : $"main.py {categories}";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonBin,
                Arguments = args,
                WorkingDirectory = Path.GetFullPath(scraperDir),
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError  = false,
                CreateNoWindow = true,
            };
            System.Diagnostics.Process.Start(psi);
            TempData["Success"] = $"Scraper đã được khởi động ({(string.IsNullOrWhiteSpace(categories) ? "tất cả" : categories)}). Kiểm tra log sau vài phút.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Không thể khởi động scraper: {ex.Message}";
        }

        return RedirectToAction(nameof(Scraper));
    }

    // ── product helpers ──────────────────────────────────────────

    private async Task<(List<ProductListItem> items, int total)> LoadProductsAsync(string category, int page, int size)
    {
        List<ProductListItem> items;
        switch (category)
        {
            case "cpu":
                var cpus = await _db.Cpus.OrderBy(c => c.Name).ToListAsync();
                items = cpus.Select(c => new ProductListItem { Id = c.Id, Category = "cpu", Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Specs = new() { ["Socket"] = c.Socket, ["Cores"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["Stock"] = c.Stock.ToString() } }).ToList();
                break;
            case "motherboard":
                var mbs = await _db.Motherboards.OrderBy(m => m.Name).ToListAsync();
                items = mbs.Select(m => new ProductListItem { Id = m.Id, Category = "motherboard", Name = m.Name, Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl, Specs = new() { ["Socket"] = m.SocketCompatibility, ["Form"] = m.FormFactor, ["Stock"] = m.Stock.ToString() } }).ToList();
                break;
            case "memory":
                var mems = await _db.Memories.OrderBy(m => m.Name).ToListAsync();
                items = mems.Select(m => new ProductListItem { Id = m.Id, Category = "memory", Name = m.Name, Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl, Specs = new() { ["Type"] = m.Type, ["Capacity"] = $"{m.Capacity}GB", ["Stock"] = m.Stock.ToString() } }).ToList();
                break;
            case "gpu":
                var gpus = await _db.VideoCards.OrderBy(g => g.Name).ToListAsync();
                items = gpus.Select(g => new ProductListItem { Id = g.Id, Category = "gpu", Name = g.Name, Manufacturer = g.Manufacturer, Price = g.Price, ImageUrl = g.ImageUrl, Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["TDP"] = $"{g.TDP}W", ["Stock"] = g.Stock.ToString() } }).ToList();
                break;
            case "psu":
                var psus = await _db.PowerSupplies.OrderBy(p => p.Name).ToListAsync();
                items = psus.Select(p => new ProductListItem { Id = p.Id, Category = "psu", Name = p.Name, Manufacturer = p.Manufacturer, Price = p.Price, ImageUrl = p.ImageUrl, Specs = new() { ["Wattage"] = $"{p.Wattage}W", ["Eff"] = p.Efficiency, ["Stock"] = p.Stock.ToString() } }).ToList();
                break;
            case "case":
                var cases = await _db.CaseEnclosures.OrderBy(c => c.Name).ToListAsync();
                items = cases.Select(c => new ProductListItem { Id = c.Id, Category = "case", Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Specs = new() { ["Form"] = c.FormFactorSupport, ["MaxGPU"] = $"{c.MaxVGALength}mm", ["Stock"] = c.Stock.ToString() } }).ToList();
                break;
            case "storage":
                var storages = await _db.Storages.OrderBy(s => s.Name).ToListAsync();
                items = storages.Select(s => new ProductListItem { Id = s.Id, Category = "storage", Name = s.Name, Manufacturer = s.Manufacturer, Price = s.Price, ImageUrl = s.ImageUrl, Specs = new() { ["Type"] = s.Type, ["Cap"] = $"{s.Capacity}GB", ["Stock"] = s.Stock.ToString() } }).ToList();
                break;
            default: // cooler
                var coolers = await _db.CpuCoolers.OrderBy(c => c.Name).ToListAsync();
                items = coolers.Select(c => new ProductListItem { Id = c.Id, Category = "cooler", Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Specs = new() { ["Type"] = c.Type, ["MaxTDP"] = $"{c.MaxTDP}W", ["Stock"] = c.Stock.ToString() } }).ToList();
                break;
        }
        int total = items.Count;
        return (items.Skip((page - 1) * size).Take(size).ToList(), total);
    }

    private async Task<AdminProductEditViewModel?> LoadProductVmAsync(string category, int id)
    {
        return category switch
        {
            "cpu" => (await _db.Cpus.FindAsync(id)) is Cpu c ? new AdminProductEditViewModel
            {
                Category = "cpu", Id = c.Id, Name = c.Name, Manufacturer = c.Manufacturer,
                Price = c.Price, Stock = c.Stock, ImageUrl = c.ImageUrl,
                Socket = c.Socket, CoreCount = c.CoreCount, ThreadCount = c.ThreadCount,
                BaseClock = c.BaseClock, BoostClock = c.BoostClock, TDP = c.TDP,
                CpuPerformance = c.ApproximatePerformance
            } : null,
            "motherboard" => (await _db.Motherboards.FindAsync(id)) is Motherboard m ? new AdminProductEditViewModel
            {
                Category = "motherboard", Id = m.Id, Name = m.Name, Manufacturer = m.Manufacturer,
                Price = m.Price, Stock = m.Stock, ImageUrl = m.ImageUrl,
                SocketCompatibility = m.SocketCompatibility, FormFactor = m.FormFactor,
                MemoryCompatibility = m.MemoryCompatibility, MemorySlots = m.MemorySlots,
                MaxMemoryCapacity = m.MaxMemoryCapacity
            } : null,
            "memory" => (await _db.Memories.FindAsync(id)) is Memory mem ? new AdminProductEditViewModel
            {
                Category = "memory", Id = mem.Id, Name = mem.Name, Manufacturer = mem.Manufacturer,
                Price = mem.Price, Stock = mem.Stock, ImageUrl = mem.ImageUrl,
                MemoryType = mem.Type, Capacity = mem.Capacity, Modules = mem.Modules, Speed = mem.Speed
            } : null,
            "gpu" => (await _db.VideoCards.FindAsync(id)) is VideoCard g ? new AdminProductEditViewModel
            {
                Category = "gpu", Id = g.Id, Name = g.Name, Manufacturer = g.Manufacturer,
                Price = g.Price, Stock = g.Stock, ImageUrl = g.ImageUrl,
                VRAM = g.VRAM, GpuLength = g.Length, TDP = g.TDP, GpuPerformance = g.ApproximatePerformance
            } : null,
            "psu" => (await _db.PowerSupplies.FindAsync(id)) is PowerSupply p ? new AdminProductEditViewModel
            {
                Category = "psu", Id = p.Id, Name = p.Name, Manufacturer = p.Manufacturer,
                Price = p.Price, Stock = p.Stock, ImageUrl = p.ImageUrl,
                Wattage = p.Wattage, Efficiency = p.Efficiency, Modular = p.Modular
            } : null,
            "case" => (await _db.CaseEnclosures.FindAsync(id)) is CaseEnclosure ce ? new AdminProductEditViewModel
            {
                Category = "case", Id = ce.Id, Name = ce.Name, Manufacturer = ce.Manufacturer,
                Price = ce.Price, Stock = ce.Stock, ImageUrl = ce.ImageUrl,
                FormFactorSupport = ce.FormFactorSupport, MaxVGALength = ce.MaxVGALength, Color = ce.Color
            } : null,
            "storage" => (await _db.Storages.FindAsync(id)) is Storage st ? new AdminProductEditViewModel
            {
                Category = "storage", Id = st.Id, Name = st.Name, Manufacturer = st.Manufacturer,
                Price = st.Price, Stock = st.Stock, ImageUrl = st.ImageUrl,
                StorageType = st.Type, StorageCapacity = st.Capacity, Interface = st.Interface,
                ReadSpeed = st.ReadSpeed, WriteSpeed = st.WriteSpeed
            } : null,
            "cooler" => (await _db.CpuCoolers.FindAsync(id)) is CpuCooler cc ? new AdminProductEditViewModel
            {
                Category = "cooler", Id = cc.Id, Name = cc.Name, Manufacturer = cc.Manufacturer,
                Price = cc.Price, Stock = cc.Stock, ImageUrl = cc.ImageUrl,
                CoolerSocketCompatibility = cc.SocketCompatibility, MaxTDP = cc.MaxTDP,
                Height = cc.Height, CoolerType = cc.Type
            } : null,
            _ => null
        };
    }

    private async Task CreateProductAsync(AdminProductEditViewModel vm)
    {
        switch (vm.Category)
        {
            case "cpu":
                _db.Cpus.Add(new Cpu { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, Socket = vm.Socket ?? "", CoreCount = vm.CoreCount ?? 0, ThreadCount = vm.ThreadCount ?? 0, BaseClock = vm.BaseClock ?? 0, BoostClock = vm.BoostClock ?? 0, TDP = vm.TDP ?? 0, ApproximatePerformance = vm.CpuPerformance ?? 0 }); break;
            case "motherboard":
                _db.Motherboards.Add(new Motherboard { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, SocketCompatibility = vm.SocketCompatibility ?? "", FormFactor = vm.FormFactor ?? "", MemoryCompatibility = vm.MemoryCompatibility ?? "", MemorySlots = vm.MemorySlots ?? 0, MaxMemoryCapacity = vm.MaxMemoryCapacity ?? 0 }); break;
            case "memory":
                _db.Memories.Add(new Memory { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, Type = vm.MemoryType ?? "", Capacity = vm.Capacity ?? 0, Modules = vm.Modules ?? 1, Speed = vm.Speed ?? 0 }); break;
            case "gpu":
                _db.VideoCards.Add(new VideoCard { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, VRAM = vm.VRAM ?? 0, Length = vm.GpuLength ?? 0, TDP = vm.TDP ?? 0, ApproximatePerformance = vm.GpuPerformance ?? 0 }); break;
            case "psu":
                _db.PowerSupplies.Add(new PowerSupply { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, Wattage = vm.Wattage ?? 0, Efficiency = vm.Efficiency ?? "", Modular = vm.Modular ?? "" }); break;
            case "case":
                _db.CaseEnclosures.Add(new CaseEnclosure { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, FormFactorSupport = vm.FormFactorSupport ?? "", MaxVGALength = vm.MaxVGALength ?? 0, Color = vm.Color }); break;
            case "storage":
                _db.Storages.Add(new Storage { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, Type = vm.StorageType ?? "", Capacity = vm.StorageCapacity ?? 0, Interface = vm.Interface ?? "", ReadSpeed = vm.ReadSpeed ?? 0, WriteSpeed = vm.WriteSpeed ?? 0 }); break;
            case "cooler":
                _db.CpuCoolers.Add(new CpuCooler { Name = vm.Name, Manufacturer = vm.Manufacturer, Price = vm.Price, Stock = vm.Stock, ImageUrl = vm.ImageUrl, SocketCompatibility = vm.CoolerSocketCompatibility ?? "", MaxTDP = vm.MaxTDP ?? 0, Height = vm.Height ?? 0, Type = vm.CoolerType ?? "" }); break;
        }
        await Task.CompletedTask;
    }

    private async Task UpdateProductAsync(AdminProductEditViewModel vm)
    {
        switch (vm.Category)
        {
            case "cpu":
                var c = await _db.Cpus.FindAsync(vm.Id); if (c == null) return;
                c.Name = vm.Name; c.Manufacturer = vm.Manufacturer; c.Price = vm.Price; c.Stock = vm.Stock; c.ImageUrl = vm.ImageUrl;
                c.Socket = vm.Socket ?? ""; c.CoreCount = vm.CoreCount ?? 0; c.ThreadCount = vm.ThreadCount ?? 0;
                c.BaseClock = vm.BaseClock ?? 0; c.BoostClock = vm.BoostClock ?? 0; c.TDP = vm.TDP ?? 0; c.ApproximatePerformance = vm.CpuPerformance ?? 0; break;
            case "motherboard":
                var mb = await _db.Motherboards.FindAsync(vm.Id); if (mb == null) return;
                mb.Name = vm.Name; mb.Manufacturer = vm.Manufacturer; mb.Price = vm.Price; mb.Stock = vm.Stock; mb.ImageUrl = vm.ImageUrl;
                mb.SocketCompatibility = vm.SocketCompatibility ?? ""; mb.FormFactor = vm.FormFactor ?? "";
                mb.MemoryCompatibility = vm.MemoryCompatibility ?? ""; mb.MemorySlots = vm.MemorySlots ?? 0; mb.MaxMemoryCapacity = vm.MaxMemoryCapacity ?? 0; break;
            case "memory":
                var mem = await _db.Memories.FindAsync(vm.Id); if (mem == null) return;
                mem.Name = vm.Name; mem.Manufacturer = vm.Manufacturer; mem.Price = vm.Price; mem.Stock = vm.Stock; mem.ImageUrl = vm.ImageUrl;
                mem.Type = vm.MemoryType ?? ""; mem.Capacity = vm.Capacity ?? 0; mem.Modules = vm.Modules ?? 1; mem.Speed = vm.Speed ?? 0; break;
            case "gpu":
                var g = await _db.VideoCards.FindAsync(vm.Id); if (g == null) return;
                g.Name = vm.Name; g.Manufacturer = vm.Manufacturer; g.Price = vm.Price; g.Stock = vm.Stock; g.ImageUrl = vm.ImageUrl;
                g.VRAM = vm.VRAM ?? 0; g.Length = vm.GpuLength ?? 0; g.TDP = vm.TDP ?? 0; g.ApproximatePerformance = vm.GpuPerformance ?? 0; break;
            case "psu":
                var p = await _db.PowerSupplies.FindAsync(vm.Id); if (p == null) return;
                p.Name = vm.Name; p.Manufacturer = vm.Manufacturer; p.Price = vm.Price; p.Stock = vm.Stock; p.ImageUrl = vm.ImageUrl;
                p.Wattage = vm.Wattage ?? 0; p.Efficiency = vm.Efficiency ?? ""; p.Modular = vm.Modular ?? ""; break;
            case "case":
                var ce = await _db.CaseEnclosures.FindAsync(vm.Id); if (ce == null) return;
                ce.Name = vm.Name; ce.Manufacturer = vm.Manufacturer; ce.Price = vm.Price; ce.Stock = vm.Stock; ce.ImageUrl = vm.ImageUrl;
                ce.FormFactorSupport = vm.FormFactorSupport ?? ""; ce.MaxVGALength = vm.MaxVGALength ?? 0; ce.Color = vm.Color; break;
            case "storage":
                var st = await _db.Storages.FindAsync(vm.Id); if (st == null) return;
                st.Name = vm.Name; st.Manufacturer = vm.Manufacturer; st.Price = vm.Price; st.Stock = vm.Stock; st.ImageUrl = vm.ImageUrl;
                st.Type = vm.StorageType ?? ""; st.Capacity = vm.StorageCapacity ?? 0; st.Interface = vm.Interface ?? "";
                st.ReadSpeed = vm.ReadSpeed ?? 0; st.WriteSpeed = vm.WriteSpeed ?? 0; break;
            case "cooler":
                var cc = await _db.CpuCoolers.FindAsync(vm.Id); if (cc == null) return;
                cc.Name = vm.Name; cc.Manufacturer = vm.Manufacturer; cc.Price = vm.Price; cc.Stock = vm.Stock; cc.ImageUrl = vm.ImageUrl;
                cc.SocketCompatibility = vm.CoolerSocketCompatibility ?? ""; cc.MaxTDP = vm.MaxTDP ?? 0;
                cc.Height = vm.Height ?? 0; cc.Type = vm.CoolerType ?? ""; break;
        }
    }

    private async Task DeleteProductAsync(string category, int id)
    {
        switch (category)
        {
            case "cpu":         var c  = await _db.Cpus.FindAsync(id);           if (c  != null) _db.Cpus.Remove(c);           break;
            case "motherboard": var mb = await _db.Motherboards.FindAsync(id);   if (mb != null) _db.Motherboards.Remove(mb);   break;
            case "memory":      var m  = await _db.Memories.FindAsync(id);       if (m  != null) _db.Memories.Remove(m);        break;
            case "gpu":         var g  = await _db.VideoCards.FindAsync(id);     if (g  != null) _db.VideoCards.Remove(g);      break;
            case "psu":         var p  = await _db.PowerSupplies.FindAsync(id);  if (p  != null) _db.PowerSupplies.Remove(p);   break;
            case "case":        var enc = await _db.CaseEnclosures.FindAsync(id); if (enc != null) _db.CaseEnclosures.Remove(enc); break;
            case "storage":     var st = await _db.Storages.FindAsync(id);       if (st != null) _db.Storages.Remove(st);       break;
            case "cooler":      var cc = await _db.CpuCoolers.FindAsync(id);     if (cc != null) _db.CpuCoolers.Remove(cc);     break;
        }
    }
}
