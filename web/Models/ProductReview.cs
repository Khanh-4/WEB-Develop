using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("product_reviews")]
public class ProductReview
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public int ComponentId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(100)]
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}

[Table("product_questions")]
public class ProductQuestion
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public int ComponentId { get; set; }

    [Required, MaxLength(500)]
    public string Question { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public List<ProductAnswer> Answers { get; set; } = new();
}

[Table("product_answers")]
public class ProductAnswer
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Answer { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UserDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductQuestion Question { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
