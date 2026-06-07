using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("flash_sales")]
public class FlashSale
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    public int ProductId { get; set; }

    [Required, MaxLength(300)]
    public string ProductName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalPrice { get; set; }

    // Discount percent 1-99
    public int DiscountPercent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    public int TotalQty { get; set; }
    public int SoldQty { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
