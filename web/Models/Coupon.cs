using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

public enum DiscountType { Percent = 0, Fixed = 1 }

[Table("coupons")]
public class Coupon
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }

    // Minimum order amount required (null = no minimum)
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinOrderAmount { get; set; }

    // Total uses allowed (null = unlimited)
    public int? MaxUses { get; set; }
    public int UsedCount { get; set; }

    // Category filter: comma-separated list like "cpu,gpu" (null = all)
    [MaxLength(200)]
    public string? CategoryFilter { get; set; }

    // Brand filter (null = all brands)
    [MaxLength(100)]
    public string? BrandFilter { get; set; }

    public bool IsFreeShip { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
