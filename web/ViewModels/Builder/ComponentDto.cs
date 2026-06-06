namespace TechSpecs.ViewModels.Builder;

/// <summary>
/// Lightweight DTO returned to frontend for each component in filtered lists.
/// </summary>
public class ComponentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public double PpScore { get; set; }        // Performance-per-Price (higher = better value)
    public bool IsCompatible { get; set; } = true;
    public string? IncompatibleReason { get; set; }
    public List<string> Badges { get; set; } = new();
    public bool IsRecommended { get; set; } = false;
    public Dictionary<string, string> Specs { get; set; } = new();
}

public class FilteredResult
{
    public List<ComponentDto> Cpus { get; set; } = new();
    public List<ComponentDto> Motherboards { get; set; } = new();
    public List<ComponentDto> Memories { get; set; } = new();
    public List<ComponentDto> VideoCards { get; set; } = new();
    public List<ComponentDto> PowerSupplies { get; set; } = new();
    public List<ComponentDto> Cases { get; set; } = new();
    public List<ComponentDto> Storages { get; set; } = new();
    public List<ComponentDto> Coolers { get; set; } = new();

    // Totals for the current selected build
    public decimal TotalPrice { get; set; }
    public int TotalTDP { get; set; }
    public int RecommendedPsuWattage { get; set; }  // (CPU.TDP + GPU.TDP) * 1.3
}
