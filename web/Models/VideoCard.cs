using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("video_card")]
public class VideoCard
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    public int VRAM { get; set; }                            // GB
    public int Length { get; set; }                          // mm — must be <= CaseEnclosure.MaxVGALength
    public int TDP { get; set; }                             // Watts

    [Column(TypeName = "decimal(10,2)")]
    public decimal ApproximatePerformance { get; set; }      // Heuristic score for P/P

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
