using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("build_upvotes")]
public class BuildUpvote
{
    public int Id { get; set; }

    [Required]
    public int BuildId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SavedBuild Build { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
