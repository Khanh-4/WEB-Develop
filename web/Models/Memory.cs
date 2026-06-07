using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("memory")]
public class Memory
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(10)]
    public string Type { get; set; } = string.Empty;   // DDR4 or DDR5 — must match Motherboard.MemoryCompatibility

    public int Capacity { get; set; }                  // GB total
    public int Modules { get; set; }                   // e.g. 2 (for 2x8GB kit)
    public int Speed { get; set; }                     // MHz

    [MaxLength(30)]
    public string Profile { get; set; } = string.Empty; // Intel XMP, AMD Expo, XMP 3.0…

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
