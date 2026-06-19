using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Services;

namespace TechSpecs.Controllers;

[Route("Quote")]
public class QuoteController : Controller
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly ILogger<QuoteController> _logger;

    public QuoteController(AppDbContext db, IEmailSender emailSender,
        IConfiguration config, ILogger<QuoteController> logger)
    {
        _db = db;
        _emailSender = emailSender;
        _config = config;
        _logger = logger;
    }

    // POST /Quote/Installment
    [HttpPost("Installment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Installment([FromBody] InstallmentRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { error = "Thiếu thông tin bắt buộc." });

        // Save to quote_requests table
        var quote = new QuoteRequest
        {
            PhoneOrEmail = $"{req.CustomerName.Trim()} | {req.Phone.Trim()}",
            Budget       = $"[Trả góp] {req.ProductName} | {req.Plan} | {req.MonthlyAmount:N0}đ/tháng | NH: {req.Bank ?? "Chưa chọn"}",
            CreatedAt    = DateTime.UtcNow,
        };
        _db.QuoteRequests.Add(quote);
        await _db.SaveChangesAsync();

        // Notify admin
        var adminEmail = _config["Notification:AdminEmail"] ?? _config["Gmail:Sender"]?.Split('<', '>').ElementAtOrDefault(1) ?? "duylamasd1995@gmail.com";
        var subject = $"[TechSpecs] Yêu cầu trả góp – {req.CustomerName}";
        var html = $@"
<div style='font-family:sans-serif;max-width:560px'>
  <h2 style='color:#7c3aed'>Yêu cầu trả góp mới</h2>
  <table cellpadding='8' style='width:100%;border-collapse:collapse'>
    <tr><td style='background:#f5f3ff;font-weight:600;width:150px'>Khách hàng</td><td>{System.Net.WebUtility.HtmlEncode(req.CustomerName)}</td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Số điện thoại</td><td>{System.Net.WebUtility.HtmlEncode(req.Phone)}</td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Sản phẩm</td><td>{System.Net.WebUtility.HtmlEncode(req.ProductName)}</td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Giá gốc</td><td>{req.Price:N0}đ</td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Gói trả góp</td><td>{System.Net.WebUtility.HtmlEncode(req.Plan)}</td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Góp/tháng</td><td><strong>{req.MonthlyAmount:N0}đ</strong></td></tr>
    <tr><td style='background:#f5f3ff;font-weight:600'>Ngân hàng</td><td>{System.Net.WebUtility.HtmlEncode(req.Bank ?? "Chưa chọn")}</td></tr>
  </table>
  <p style='color:#6b7280;font-size:.85rem;margin-top:16px'>Liên hệ lại khách hàng sớm nhất có thể (trong vòng 30 phút).</p>
</div>";

        try { await _emailSender.SendEmailAsync(adminEmail, subject, html); }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to send installment notification email"); }

        return Ok(new { success = true });
    }
}

public class InstallmentRequest
{
    public string ProductName   { get; set; } = "";
    public decimal Price        { get; set; }
    public string Plan          { get; set; } = "";
    public decimal MonthlyAmount{ get; set; }
    public string CustomerName  { get; set; } = "";
    public string Phone         { get; set; } = "";
    public string? Bank         { get; set; }
}
