using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("warranty_records")]
public class WarrantyRecord
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required, MaxLength(300)]
    public string ProductName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public int ComponentId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime PurchaseDate { get; set; }

    public int WarrantyMonths { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    public Order? Order { get; set; }
}
