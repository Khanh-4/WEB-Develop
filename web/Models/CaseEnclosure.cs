using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("case_enclosure")]
public class CaseEnclosure
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(50)]
    public string FormFactorSupport { get; set; } = string.Empty;  // ATX / mATX / ITX (comma-separated if multiple)

    public int MaxVGALength { get; set; }                          // mm — VideoCard.Length must be <=

    [MaxLength(30)]
    public string? Color { get; set; }

    [MaxLength(20)]
    public string CaseType { get; set; } = string.Empty;           // Mid Tower / Full Tower / Mini Tower

    [MaxLength(50)]
    public string RadiatorSupport { get; set; } = string.Empty;    // "240mm, 360mm" etc.

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
