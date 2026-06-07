using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("motherboard")]
public class Motherboard
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [Required, MaxLength(50)]
    public string SocketCompatibility { get; set; } = string.Empty;  // Must match Cpu.Socket

    [Required, MaxLength(20)]
    public string FormFactor { get; set; } = string.Empty;           // ATX, mATX, ITX

    [Required, MaxLength(10)]
    public string MemoryCompatibility { get; set; } = string.Empty;  // DDR4 or DDR5

    public int MemorySlots { get; set; }
    public int MaxMemoryCapacity { get; set; }                       // GB

    [MaxLength(20)]
    public string Chipset { get; set; } = string.Empty;             // Z790, B650, X870…

    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
}
