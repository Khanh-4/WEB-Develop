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
    // Phone is required. orderId is optional:
    //  - phone only        → all orders for that phone, newest first
    //  - orderId + phone    → that single order (backward compatible)
    //
    // SECURITY NOTE (deliberate, accepted by product owner 2026-06-17):
    // Phone-only lookup is an intentional convenience for guests (same UX as
    // Shopee/GHN order tracking by phone). The IDOR/enumeration tradeoff was
    // reviewed and accepted. Risk is bounded by data minimization: this endpoint
    // returns only order status/items/totals — never recipient name or shipping
    // address (those stay behind [Authorize] on OrdersController). Do not add
    // name/address fields here without revisiting this decision.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Check(int orderId, string phone)
    {
        phone = (phone ?? "").Trim();
        if (string.IsNullOrEmpty(phone))
            return Json(new { error = "Vui lòng nhập số điện thoại." });

        var query = _db.Orders
            .AsNoTracking()
            .Include(o => o.Details)
            .Where(o => o.Phone == phone);

        if (orderId > 0)
            query = query.Where(o => o.Id == orderId);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .ToListAsync();

        if (orders.Count == 0)
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

        var result = orders.Select(order => new
        {
            orderId = order.Id,
            status = order.Status.ToString(),
            isCancelled = order.Status == OrderStatus.Cancelled,
            currentStep = (int)order.Status,
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

        return Json(new { found = true, steps, orders = result });
    }
}
