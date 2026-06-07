using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("storage")]
public class Storage
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(20)]
    public string Type { get; set; } = string.Empty;        // SSD / HDD / NVMe

    public int Capacity { get; set; }                       // GB

    [MaxLength(20)]
    public string Interface { get; set; } = string.Empty;   // SATA / M.2 / PCIe

    public int ReadSpeed { get; set; }                      // MB/s
    public int WriteSpeed { get; set; }                     // MB/s

    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
