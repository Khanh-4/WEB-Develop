using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Services;

/// <summary>Giỏ hàng lưu trong Session — survive qua nhiều request, theo từng user.</summary>
public class CartService
{
    private const string CartKey = "TechStore.Cart";
    private readonly IHttpContextAccessor _http;
    private readonly IProductRepository _products;

    public CartService(IHttpContextAccessor http, IProductRepository products)
    {
        _http = http;
        _products = products;
    }

    private ISession Session => _http.HttpContext!.Session;

    public ShoppingCart GetCart()
        => Session.GetObject<ShoppingCart>(CartKey) ?? new ShoppingCart();

    private void Save(ShoppingCart cart) => Session.SetObject(CartKey, cart);

    public ShoppingCart Add(int productId, int quantity = 1)
    {
        var product = _products.GetById(productId)
            ?? throw new InvalidOperationException("Sản phẩm không tồn tại.");

        var cart = GetCart();
        cart.Add(new CartItem
        {
            ProductId = product.Id,
            Name = product.Name,
            Price = product.Price,
            Quantity = Math.Max(1, quantity),
            ImageUrl = product.ImageUrl
        });
        Save(cart);
        return cart;
    }

    public ShoppingCart Update(int productId, int quantity)
    {
        var cart = GetCart();
        cart.Update(productId, quantity);
        Save(cart);
        return cart;
    }

    public ShoppingCart Remove(int productId)
    {
        var cart = GetCart();
        cart.Remove(productId);
        Save(cart);
        return cart;
    }

    public void Clear()
    {
        var cart = new ShoppingCart();
        Save(cart);
    }
}
