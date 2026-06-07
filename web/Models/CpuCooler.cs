using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("cpu_cooler")]
public class CpuCooler
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(200)]
    public string SocketCompatibility { get; set; } = string.Empty;  // Comma-separated: "LGA1700,AM5,AM4"

    public int MaxTDP { get; set; }      // Max cooling capacity in watts
    public int Height { get; set; }      // mm — for case clearance check

    [MaxLength(30)]
    public string Type { get; set; } = string.Empty;   // Air / AIO-240 / AIO-360

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
