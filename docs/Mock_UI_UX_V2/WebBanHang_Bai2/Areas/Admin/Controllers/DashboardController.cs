using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IOrderRepository _orders;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IProductRepository products, ICategoryRepository categories,
        IOrderRepository orders, UserManager<ApplicationUser> userManager)
    {
        _products = products;
        _categories = categories;
        _orders = orders;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        var orders = _orders.GetAll().ToList();
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var labels = new List<string>();
        var revSeries = new List<decimal>();
        var ordSeries = new List<int>();
        for (var d = 13; d >= 0; d--)
        {
            var day = today.AddDays(-d);
            labels.Add(day.ToString("dd/MM"));
            var ordersOfDay = orders.Where(o => o.CreatedAt.Date == day).ToList();
            revSeries.Add(ordersOfDay.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total));
            ordSeries.Add(ordersOfDay.Count);
        }

        var prods = _products.GetAll().ToList();
        var catLabels = _categories.GetAllCategories().Select(c => c.Name).ToList();
        var catCounts = _categories.GetAllCategories()
            .Select(c => prods.Count(p => p.CategoryId == c.Id))
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalProducts = prods.Count,
            TotalCategories = catLabels.Count,
            TotalOrders = orders.Count,
            TotalUsers = _userManager.Users.Count(),
            OrdersToday = orders.Count(o => o.CreatedAt.Date == today),
            RevenueMonth = orders.Where(o => o.CreatedAt >= monthStart && o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
            RevenueAll = orders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
            RecentOrders = orders.Take(8).ToList(),
            TopProducts = prods.OrderByDescending(p => p.Sold).Take(5).ToList(),
            RevenueLabels = labels,
            RevenueSeries = revSeries,
            OrderSeries = ordSeries,
            CategoryLabels = catLabels,
            CategoryProductCounts = catCounts
        };
        return View(vm);
    }
}
