using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Services;

namespace WebBanHang_Bai2.Controllers;

/// <summary>Quản lý giỏ hàng — trả về JSON cho AJAX hoặc View cho trang Cart.</summary>
public class CartController : Controller
{
    private readonly CartService _cart;

    public CartController(CartService cart) => _cart = cart;

    public IActionResult Index() => View(_cart.GetCart());

    [HttpGet]
    public IActionResult Json() => new JsonResult(_cart.GetCart());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(int productId, int quantity = 1)
    {
        try
        {
            var cart = _cart.Add(productId, quantity);
            return new JsonResult(cart);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity)
        => new JsonResult(_cart.Update(productId, quantity));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
        => new JsonResult(_cart.Remove(productId));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        _cart.Clear();
        return new JsonResult(_cart.GetCart());
    }
}
