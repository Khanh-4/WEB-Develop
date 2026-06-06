using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("carts")]
public class Cart
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public List<CartItem> Items { get; set; } = new();
}

[Table("cart_items")]
public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;  // "cpu", "gpu", etc.

    public int ComponentId { get; set; }

    [Required, MaxLength(300)]
    public string ComponentName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ImageUrl { get; set; }

    public Cart Cart { get; set; } = null!;
}
