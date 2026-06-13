using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechSpecs.Models;

[Table("product_articles")]
[Index(nameof(Url), IsUnique = true)]
public class ProductArticle
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? ProductCategory { get; set; }

    public int? ProductId { get; set; }

    [Required, MaxLength(20)]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? ThumbnailUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public double? MatchScore { get; set; }
}
