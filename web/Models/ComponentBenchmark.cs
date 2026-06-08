using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("component_benchmarks")]
public class ComponentBenchmark
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Category { get; set; } = string.Empty;       // "cpu" or "gpu"

    [Required, MaxLength(200)]
    public string ComponentName { get; set; } = string.Empty;  // partial ILIKE match

    // CPU benchmarks
    public int? CinebenchR23Multi { get; set; }
    public int? CinebenchR23Single { get; set; }

    // GPU benchmarks (FPS @ High/Ultra)
    public int? FpsCs2_1080p { get; set; }
    public int? FpsCs2_1440p { get; set; }
    public int? FpsCyberpunk_1080p { get; set; }
    public int? FpsCyberpunk_1440p { get; set; }
    public int? FpsValorant_1080p { get; set; }
    public int? FpsValorant_1440p { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
