using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Controllers;

public class OrderTrackingController : Controller
{
    private readonly AppDbContext _db;

    public OrderTrackingController(AppDbContext db) => _db = db;

    // GET /OrderTracking
    public IActionResult Index() => View();

    // POST /OrderTracking/Check
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(int orderId, string phone)
    {
        phone = (phone ?? "").Trim();
        if (orderId <= 0 || string.IsNullOrEmpty(phone))
            return Json(new { error = "Vui lòng nhập đầy đủ thông tin." });

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.Phone == phone);

        if (order == null)
            return Json(new { found = false });

        var steps = new[]
        {
            new { status = (int)OrderStatus.Pending,      label = "Chờ xử lý",     icon = "bi-clock" },
            new { status = (int)OrderStatus.Confirmed,    label = "Đã xác nhận",   icon = "bi-check-circle" },
            new { status = (int)OrderStatus.Assembling,   label = "Đang lắp ráp",  icon = "bi-tools" },
            new { status = (int)OrderStatus.InstallingOS, label = "Cài đặt hệ điều hành", icon = "bi-display" },
            new { status = (int)OrderStatus.Shipped,      label = "Đang giao",      icon = "bi-truck" },
            new { status = (int)OrderStatus.Delivered,    label = "Đã nhận hàng",  icon = "bi-house-check" },
        };

        int currentStep = (int)order.Status;
        bool isCancelled = order.Status == OrderStatus.Cancelled;

        return Json(new
        {
            found = true,
            orderId = order.Id,
            status = order.Status.ToString(),
            isCancelled,
            currentStep,
            steps,
            totalAmount = order.TotalAmount,
            discountAmount = order.DiscountAmount,
            paymentMethod = order.PaymentMethod.ToString(),
            createdAt = order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            items = order.Details.Select(d => new
            {
                d.ComponentName, d.Category, d.Quantity,
                price = d.Price,
                subtotal = d.Price * d.Quantity,
                d.ImageUrl
            })
        });
    }
}
