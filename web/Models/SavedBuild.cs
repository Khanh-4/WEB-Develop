using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("saved_builds")]
public class SavedBuild
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string BuildJson { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string ShareToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsPublic { get; set; } = false;
    public int  UpvoteCount { get; set; } = 0;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<BuildUpvote> Upvotes { get; set; } = new List<BuildUpvote>();
}
