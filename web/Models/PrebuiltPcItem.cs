namespace TechSpecs.Models;

public class PrebuiltPcItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty; // e.g. "Gaming", "Office", "Creator"
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    
    // Key specs for badges
    public string CpuBadge { get; set; } = string.Empty;
    public string GpuBadge { get; set; } = string.Empty;
    public string RamBadge { get; set; } = string.Empty;
    public string StorageBadge { get; set; } = string.Empty;
}
