using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("cpu")]
public class Cpu
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(50)]
    public string Socket { get; set; } = string.Empty;       // LGA1700, AM5, AM4...

    public int CoreCount { get; set; }
    public int ThreadCount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal BaseClock { get; set; }                   // GHz

    [Column(TypeName = "decimal(5,2)")]
    public decimal BoostClock { get; set; }                  // GHz

    public int TDP { get; set; }                             // Watts

    [Column(TypeName = "decimal(10,2)")]
    public decimal ApproximatePerformance { get; set; }      // Heuristic score for P/P

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
