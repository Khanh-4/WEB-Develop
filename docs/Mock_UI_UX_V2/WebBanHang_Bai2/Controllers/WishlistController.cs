using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Repositories;
using WebBanHang_Bai2.Services;

namespace WebBanHang_Bai2.Controllers;

public class WishlistController : Controller
{
    private readonly WishlistService _wishlist;
    private readonly IProductRepository _products;

    public WishlistController(WishlistService wishlist, IProductRepository products)
    {
        _wishlist = wishlist;
        _products = products;
    }

    public IActionResult Index()
    {
        var ids = _wishlist.GetIds();
        var items = _products.GetAll().Where(p => ids.Contains(p.Id)).ToList();
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Toggle(int productId)
    {
        var added = _wishlist.Toggle(productId);
        return new JsonResult(new { added, count = _wishlist.Count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        _wishlist.Remove(productId);
        TempData["Success"] = "Đã xoá khỏi danh sách yêu thích.";
        return RedirectToAction(nameof(Index));
    }
}
