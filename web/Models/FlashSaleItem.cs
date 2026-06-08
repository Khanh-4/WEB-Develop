namespace TechSpecs.Models;

public class FlashSaleItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public DateTime EndTime { get; set; }
}
