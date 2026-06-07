using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;

namespace TechSpecs.Controllers;

public class WarrantyController : Controller
{
    private readonly AppDbContext _db;

    public WarrantyController(AppDbContext db) => _db = db;

    // GET /Warranty
    public IActionResult Index() => View();

    // POST /Warranty/Check
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(string query)
    {
        query = (query ?? "").Trim();
        if (string.IsNullOrEmpty(query))
            return Json(new { error = "Vui lòng nhập số điện thoại hoặc số serial." });

        var records = await _db.WarrantyRecords
            .AsNoTracking()
            .Where(w => w.Phone == query || w.SerialNumber == query)
            .OrderByDescending(w => w.PurchaseDate)
            .Select(w => new
            {
                w.ProductName,
                w.Category,
                w.ImageUrl,
                w.SerialNumber,
                PurchaseDate   = w.PurchaseDate.ToString("dd/MM/yyyy"),
                ExpiryDate     = w.PurchaseDate.AddMonths(w.WarrantyMonths).ToString("dd/MM/yyyy"),
                ExpiryDateTime = w.PurchaseDate.AddMonths(w.WarrantyMonths),
                w.WarrantyMonths,
                w.OrderId,
            })
            .ToListAsync();

        if (records.Count == 0)
            return Json(new { found = false });

        var now = DateTime.UtcNow;
        var result = records.Select(r => new
        {
            r.ProductName,
            r.Category,
            r.ImageUrl,
            r.SerialNumber,
            r.PurchaseDate,
            r.ExpiryDate,
            r.WarrantyMonths,
            r.OrderId,
            DaysLeft = (int)(r.ExpiryDateTime - now).TotalDays,
            IsExpired = r.ExpiryDateTime < now,
        });

        return Json(new { found = true, records = result });
    }
}
