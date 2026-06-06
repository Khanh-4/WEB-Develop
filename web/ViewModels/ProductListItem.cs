namespace TechSpecs.ViewModels;

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public int Stock { get; set; }
    public Dictionary<string, string> Specs { get; set; } = new();
}

public class ProductListItem
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;   // "cpu", "gpu", etc.
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public double PpScore { get; set; }
    public Dictionary<string, string> Specs { get; set; } = new();
}

public class ProductsIndexViewModel
{
    public List<ProductListItem> Products { get; set; } = new();
    public string SelectedCategory { get; set; } = "all";
    public string? SearchQuery { get; set; }
    public string SortBy { get; set; } = "pp";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}
