using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Controllers;

/// <summary>Trang chi tiết sản phẩm + tiếp nhận đánh giá.</summary>
public class ProductController : Controller
{
    private readonly IProductRepository _products;
    private readonly IReviewRepository _reviews;

    public ProductController(IProductRepository products, IReviewRepository reviews)
    {
        _products = products;
        _reviews = reviews;
    }

    public IActionResult Detail(int id)
    {
        var product = _products.GetById(id);
        if (product is null) return NotFound();

        var related = _products.GetAll()
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .Take(4).ToList();

        var vm = new ProductDetailViewModel
        {
            Product = product,
            Related = related,
            Reviews = _reviews.GetByProduct(id).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Review(Review review)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Đánh giá không hợp lệ. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Detail), new { id = review.ProductId });
        }

        _reviews.Add(review);

        // Cập nhật rating trung bình + review count cho sản phẩm
        var all = _reviews.GetByProduct(review.ProductId).ToList();
        var p = _products.GetById(review.ProductId);
        if (p is not null && all.Count > 0)
        {
            p.Rating = Math.Round(all.Average(r => r.Rating), 1);
            p.ReviewCount = all.Count;
            _products.Update(p);
        }

        TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
        return RedirectToAction(nameof(Detail), new { id = review.ProductId });
    }
}
