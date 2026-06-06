using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled
}

[Table("orders")]
public class Order
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Required, MaxLength(200)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public List<OrderDetail> Details { get; set; } = new();
}

[Table("order_details")]
public class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public int ComponentId { get; set; }

    [Required, MaxLength(300)]
    public string ComponentName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ImageUrl { get; set; }

    public Order Order { get; set; } = null!;
}
