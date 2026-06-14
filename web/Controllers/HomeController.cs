using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Services;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IMockDataService _mockDataService;
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly IConfiguration _config;

    public HomeController(
        ILogger<HomeController> logger,
        IMockDataService mockDataService,
        AppDbContext db,
        IEmailSender email,
        IConfiguration config)
    {
        _logger = logger;
        _mockDataService = mockDataService;
        _db = db;
        _email = email;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        // Spotlight 1: soonest-expiring active flash sale
        var spotlightFlash = await _db.FlashSales
            .Where(f => f.IsActive && f.StartsAt <= now && f.EndsAt > now)
            .OrderBy(f => f.EndsAt)
            .FirstOrDefaultAsync();

        // Spotlight 2: newest product by highest Id across CPU and GPU tables
        var newestCpu = await _db.Cpus
            .OrderByDescending(x => x.Id)
            .Select(x => new SpotlightProductDto(x.Id, "cpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync();
        var newestGpu = await _db.VideoCards
            .OrderByDescending(x => x.Id)
            .Select(x => new SpotlightProductDto(x.Id, "gpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync();
        var spotlightNew = (newestCpu, newestGpu) switch
        {
            (null, var g) => g,
            (var c, null) => c,
            (var c, var g) => c.Id > g.Id ? c : g,
        };

        // Spotlight 3: best-value GPU by ApproximatePerformance/Price; fallback to CPU
        // GPU and CPU scores use different scales so we don't cross-compare — GPU wins by default
        var spotlightHot = await _db.VideoCards
            .Where(x => x.Price > 0 && x.ApproximatePerformance > 0)
            .OrderByDescending(x => x.ApproximatePerformance / x.Price)
            .Select(x => new SpotlightProductDto(x.Id, "gpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync()
            ?? await _db.Cpus
                .Where(x => x.Price > 0 && x.ApproximatePerformance > 0)
                .OrderByDescending(x => x.ApproximatePerformance / x.Price)
                .Select(x => new SpotlightProductDto(x.Id, "cpu", x.Name, x.Price, x.ImageUrl))
                .FirstOrDefaultAsync();

        var vm = new HomeViewModel
        {
            PrebuiltPcs    = _mockDataService.GetPrebuiltPcs(),
            SpotlightFlash = spotlightFlash,
            SpotlightNew   = spotlightNew,
            SpotlightHot   = spotlightHot,
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickQuote(string? website, string? phoneOrEmail, string? budget)
    {
        // Honeypot check — bots fill the hidden "website" field
        if (!string.IsNullOrEmpty(website))
            return RedirectToAction("Index");

        if (string.IsNullOrWhiteSpace(phoneOrEmail))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập số điện thoại hoặc email.";
            return RedirectToAction("Index");
        }

        // 1. Save to DB
        var quote = new QuoteRequest
        {
            PhoneOrEmail = phoneOrEmail.Trim(),
            Budget = string.IsNullOrWhiteSpace(budget) ? null : budget.Trim(),
        };
        _db.QuoteRequests.Add(quote);
        await _db.SaveChangesAsync();

        // 2. Notify admin by email — fire-and-forget, never crash the user request
        var adminEmail = _config["Notification:AdminEmail"];
        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var capturedLogger = _logger;
            _ = Task.Run(async () =>
            {
                try
                {
                    var html = $"""
                        <h2>Yêu cầu báo giá mới #{quote.Id}</h2>
                        <table cellpadding="8" style="border-collapse:collapse;font-family:sans-serif">
                          <tr><td><b>Liên hệ</b></td><td>{System.Net.WebUtility.HtmlEncode(quote.PhoneOrEmail)}</td></tr>
                          <tr><td><b>Ngân sách</b></td><td>{System.Net.WebUtility.HtmlEncode(quote.Budget ?? "—")}</td></tr>
                          <tr><td><b>Thời gian</b></td><td>{quote.CreatedAt:dd/MM/yyyy HH:mm} UTC</td></tr>
                        </table>
                        <p><a href="{baseUrl}/Admin/QuoteRequests">Xem tất cả yêu cầu →</a></p>
                        """;
                    await _email.SendEmailAsync(adminEmail, $"[TechSpecs] Báo giá #{quote.Id} — {quote.PhoneOrEmail}", html);
                    capturedLogger.LogInformation("Quote notification email sent to {AdminEmail} for QuoteRequest #{Id}", adminEmail, quote.Id);
                }
                catch (Exception ex)
                {
                    capturedLogger.LogWarning(ex, "Failed to send quote notification email for QuoteRequest #{Id}", quote.Id);
                }
            });
        }
        else
        {
            _logger.LogWarning("Notification:AdminEmail not configured — skipping email for QuoteRequest #{Id}", quote.Id);
        }

        TempData["SuccessMessage"] = "Yêu cầu báo giá của bạn đã được gửi. Chúng tôi sẽ liên hệ sớm nhất!";
        return RedirectToAction("Index");
    }

    public IActionResult Privacy() => View();
    public IActionResult About() => View();
    public IActionResult Contact() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
