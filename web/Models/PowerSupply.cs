using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("power_supply")]
public class PowerSupply
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    public int Wattage { get; set; }        // Must be >= (Cpu.TDP + VideoCard.TDP) * 1.3

    [MaxLength(30)]
    public string Efficiency { get; set; } = string.Empty;  // 80+ Bronze/Gold/Platinum/Titanium

    [MaxLength(20)]
    public string Modular { get; set; } = string.Empty;     // Full / Semi / Non

    [MaxLength(10)]
    public string PsuFormFactor { get; set; } = "ATX";      // ATX / SFX / SFX-L / TFX

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
