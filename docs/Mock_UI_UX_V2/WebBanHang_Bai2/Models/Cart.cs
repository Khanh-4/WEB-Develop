namespace WebBanHang_Bai2.Models;

public class CartItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }

    public decimal LineTotal => Price * Quantity;
}

public class ShoppingCart
{
    public List<CartItem> Items { get; set; } = new();

    public int TotalQuantity => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal ShippingFee => Subtotal >= 500_000 || Subtotal == 0 ? 0 : 30_000;
    public decimal Total => Subtotal + ShippingFee;

    public void Add(CartItem item)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existing is null)
        {
            Items.Add(item);
        }
        else
        {
            existing.Quantity += item.Quantity;
        }
    }

    public void Update(int productId, int quantity)
    {
        var existing = Items.FirstOrDefault(i => i.ProductId == productId);
        if (existing is null) return;
        if (quantity <= 0) Items.Remove(existing);
        else existing.Quantity = quantity;
    }

    public void Remove(int productId) => Items.RemoveAll(i => i.ProductId == productId);

    public void Clear() => Items.Clear();
}
