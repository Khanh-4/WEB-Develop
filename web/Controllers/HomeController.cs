using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<HomeController> _logger;
    private readonly TechSpecs.Services.IMockDataService _mockDataService;

    public HomeController(AppDbContext db, ILogger<HomeController> logger, TechSpecs.Services.IMockDataService mockDataService)
    {
        _db = db;
        _logger = logger;
        _mockDataService = mockDataService;
    }

    public async Task<IActionResult> Index()
    {
        const int take = 8;
        var vm = new HomeViewModel();

        vm.Categories["cpu"] = (await _db.Cpus.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(take).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cpu", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Socket"] = c.Socket, ["Cores"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["TDP"] = $"{c.TDP}W" }
            }).ToList();

        vm.Categories["gpu"] = (await _db.VideoCards.AsNoTracking()
            .Where(g => g.Price > 0 && g.ImageUrl != null)
            .OrderByDescending(g => g.Price).Take(take).ToListAsync())
            .Select(g => new ProductListItem
            {
                Id = g.Id, Category = "gpu", Name = g.Name,
                Manufacturer = g.Manufacturer, Price = g.Price, ImageUrl = g.ImageUrl,
                Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["TDP"] = $"{g.TDP}W" }
            }).ToList();

        vm.Categories["memory"] = (await _db.Memories.AsNoTracking()
            .Where(m => m.Price > 0 && m.ImageUrl != null)
            .OrderByDescending(m => m.Price).Take(take).ToListAsync())
            .Select(m => new ProductListItem
            {
                Id = m.Id, Category = "memory", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                Specs = new() { ["Type"] = m.Type, ["Capacity"] = $"{m.Capacity}GB", ["Speed"] = $"{m.Speed}MHz" }
            }).ToList();

        vm.Categories["motherboard"] = (await _db.Motherboards.AsNoTracking()
            .Where(m => m.Price > 0 && m.ImageUrl != null)
            .OrderByDescending(m => m.Price).Take(take).ToListAsync())
            .Select(m => new ProductListItem
            {
                Id = m.Id, Category = "motherboard", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                Specs = new() { ["Socket"] = m.SocketCompatibility, ["Form"] = m.FormFactor, ["RAM"] = m.MemoryCompatibility }
            }).ToList();

        vm.Categories["storage"] = (await _db.Storages.AsNoTracking()
            .Where(s => s.Price > 0 && s.ImageUrl != null)
            .OrderByDescending(s => s.Price).Take(take).ToListAsync())
            .Select(s => new ProductListItem
            {
                Id = s.Id, Category = "storage", Name = s.Name,
                Manufacturer = s.Manufacturer, Price = s.Price, ImageUrl = s.ImageUrl,
                Specs = new() { ["Type"] = s.Type, ["Capacity"] = $"{s.Capacity}GB", ["Interface"] = s.Interface }
            }).ToList();

        vm.Categories["psu"] = (await _db.PowerSupplies.AsNoTracking()
            .Where(p => p.Price > 0 && p.ImageUrl != null)
            .OrderByDescending(p => p.Price).Take(take).ToListAsync())
            .Select(p => new ProductListItem
            {
                Id = p.Id, Category = "psu", Name = p.Name,
                Manufacturer = p.Manufacturer, Price = p.Price, ImageUrl = p.ImageUrl,
                Specs = new() { ["Wattage"] = $"{p.Wattage}W", ["Efficiency"] = p.Efficiency }
            }).ToList();

        vm.Categories["case"] = (await _db.CaseEnclosures.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(take).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "case", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Form Factor"] = c.FormFactorSupport, ["Max GPU"] = $"{c.MaxVGALength}mm" }
            }).ToList();

        vm.Categories["cooler"] = (await _db.CpuCoolers.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(take).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cooler", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Type"] = c.Type, ["Max TDP"] = $"{c.MaxTDP}W" }
            }).ToList();

        vm.FlashSales = _mockDataService.GetFlashSales();
        vm.PrebuiltPcs = _mockDataService.GetPrebuiltPcs();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult QuickQuote(string? website, string? phoneOrEmail, string? budget)
    {
        // Honeypot check
        if (!string.IsNullOrEmpty(website))
        {
            // Bot detected, pretend success
            return RedirectToAction("Index");
        }

        // Logic to process the quote request would go here
        TempData["SuccessMessage"] = "Yêu cầu báo giá của bạn đã được gửi. Chúng tôi sẽ liên hệ sớm nhất!";
        return RedirectToAction("Index");
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
