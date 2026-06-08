namespace TechSpecs.ViewModels;

public class HomeViewModel
{
    public Dictionary<string, List<ProductListItem>> Categories { get; set; } = new();
    public List<TechSpecs.Models.FlashSaleItem> FlashSales { get; set; } = new();
    public List<TechSpecs.Models.PrebuiltPcItem> PrebuiltPcs { get; set; } = new();
}
