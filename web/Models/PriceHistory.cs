using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("price_history")]
public class PriceHistory
{
    public int Id { get; set; }

    [Required, MaxLength(30)]
    public string Category { get; set; } = string.Empty;   // cpu, gpu, ram, motherboard, psu, case, storage, cooler

    [Required, MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
