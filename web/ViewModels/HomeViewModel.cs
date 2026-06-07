namespace TechSpecs.ViewModels;

public class HomeViewModel
{
    public Dictionary<string, List<ProductListItem>> Categories { get; set; } = new();
}
