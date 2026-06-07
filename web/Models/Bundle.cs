using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("bundles")]
public class Bundle
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Discount: percent (0-99) applied to total bundle price
    public int DiscountPercent { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<BundleItem> Items { get; set; } = [];
}

[Table("bundle_items")]
public class BundleItem
{
    public int Id { get; set; }
    public int BundleId { get; set; }

    [Required, MaxLength(20)]
    public string Category { get; set; } = string.Empty;

    public int ComponentId { get; set; }

    [Required, MaxLength(300)]
    public string ProductName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public Bundle? Bundle { get; set; }
}
