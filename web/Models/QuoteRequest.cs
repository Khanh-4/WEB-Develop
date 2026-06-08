using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("quote_requests")]
public class QuoteRequest
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string PhoneOrEmail { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Budget { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsContacted { get; set; } = false;
}
