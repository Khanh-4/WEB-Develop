using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly TechSpecs.Services.IMockDataService _mockDataService;

    public HomeController(ILogger<HomeController> logger, TechSpecs.Services.IMockDataService mockDataService)
    {
        _logger = logger;
        _mockDataService = mockDataService;
    }

    public IActionResult Index()
    {
        // Category product sections are rendered by FeaturedCategoryViewComponent (each handles its own DB query)
        // Flash Sale section is rendered by FlashSaleViewComponent
        var vm = new HomeViewModel
        {
            PrebuiltPcs = _mockDataService.GetPrebuiltPcs()
        };
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
