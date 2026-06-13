namespace TechSpecs.ViewModels;

public class ProductArticleDto
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
}
