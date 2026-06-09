using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Controllers;

public class HomeController : Controller
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;

    public HomeController(IProductRepository products, ICategoryRepository categories)
    {
        _products = products;
        _categories = categories;
    }

    public IActionResult Index()
    {
        var all = _products.GetAll().ToList();
        ViewBag.Featured = all.Where(p => p.IsHot).Take(8).ToList();
        ViewBag.NewArrivals = all.OrderByDescending(p => p.CreatedAt).Take(8).ToList();
        ViewBag.TopSellers = all.OrderByDescending(p => p.Sold).Take(8).ToList();
        ViewBag.Categories = _categories.GetAllCategories().ToList();
        return View();
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
        => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
