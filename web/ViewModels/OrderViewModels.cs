using TechSpecs.Models;

namespace TechSpecs.ViewModels;

public class OrderListViewModel
{
    public List<OrderSummaryViewModel> Orders { get; set; } = new();
}

public class OrderSummaryViewModel
{
    public int Id { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}

public class OrderDetailViewModel
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderDetailItemViewModel> Items { get; set; } = new();
}

public class OrderDetailItemViewModel
{
    public string Category { get; set; } = string.Empty;
    public int ComponentId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Subtotal => Price * Quantity;
}
