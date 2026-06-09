using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly IOrderRepository _orders;
    public OrdersController(IOrderRepository orders) => _orders = orders;

    public IActionResult Index(string? keyword, OrderStatus? status)
    {
        var query = _orders.GetAll();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(o =>
                o.OrderCode.ToLowerInvariant().Contains(kw) ||
                o.CustomerName.ToLowerInvariant().Contains(kw) ||
                o.Phone.Contains(kw));
        }
        ViewBag.Keyword = keyword;
        ViewBag.Status = status;
        return View(query.ToList());
    }

    public IActionResult Detail(int id)
    {
        var o = _orders.GetById(id);
        return o is null ? NotFound() : View(o);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateStatus(int id, OrderStatus status)
    {
        _orders.UpdateStatus(id, status);
        TempData["Success"] = $"Đã cập nhật trạng thái đơn #{id} thành {status}.";
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _orders.Delete(id);
        TempData["Success"] = "Đã xoá đơn hàng.";
        return RedirectToAction(nameof(Index));
    }
}
