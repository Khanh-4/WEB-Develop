namespace TechSpecs.ViewModels;

public class ProductDetailViewModel
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public int Stock { get; set; }
    public Dictionary<string, string> Specs { get; set; } = new();
}

// 0=InStock 1=LowStock 2=OutOfStock 3=ComingSoon 4=ContactUs
public static class StockStatus
{
    public const int InStock    = 0;
    public const int LowStock   = 1;
    public const int OutOfStock = 2;
    public const int ComingSoon = 3;
    public const int ContactUs  = 4;

    public static int Resolve(int stock, int? overrideVal) =>
        overrideVal ?? (stock == 0 ? OutOfStock : stock <= 5 ? LowStock : InStock);

    public static (string label, string style) Badge(int status) => status switch
    {
        OutOfStock => ("Hết hàng",  "background:rgba(239,68,68,.15);color:#f87171"),
        LowStock   => ("Còn ít",    "background:rgba(251,146,60,.15);color:#fb923c"),
        ComingSoon => ("Sắp về",    "background:rgba(96,165,250,.15);color:#60a5fa"),
        ContactUs  => ("Liên hệ",  "background:rgba(168,85,247,.15);color:#c084fc"),
        _          => ("Còn hàng",  "background:rgba(34,197,94,.15);color:#4ade80"),
    };
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
    public bool IsPrebuilt { get; set; }
    public int Stock { get; set; }
    public int? StockStatusOverride { get; set; }
    public int EffectiveStockStatus => StockStatus.Resolve(Stock, StockStatusOverride);
    public Dictionary<string, string> Specs { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> FilterData { get; set; } = new();
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
