using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;

    public ReviewsController(IReviewRepository reviews, IProductRepository products)
    {
        _reviews = reviews;
        _products = products;
    }

    public IActionResult Index()
    {
        var prods = _products.GetAll().ToDictionary(p => p.Id, p => p.Name);
        ViewBag.ProductNames = prods;
        return View(_reviews.GetAll().ToList());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _reviews.Delete(id);
        TempData["Success"] = "Đã xoá đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}
