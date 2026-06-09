namespace WebBanHang_Bai2.Models;

public class ShopViewModel
{
    public IList<Product> Products { get; set; } = new List<Product>();
    public IList<Category> Categories { get; set; } = new List<Category>();
    public int? CategoryId { get; set; }
    public string? Keyword { get; set; }
    public string Sort { get; set; } = "newest";
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(PageSize, 1));
}

public class ProductDetailViewModel
{
    public Product Product { get; set; } = null!;
    public IList<Product> Related { get; set; } = new List<Product>();
    public IList<Review> Reviews { get; set; } = new List<Review>();
}

public class DashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int OrdersToday { get; set; }
    public decimal RevenueMonth { get; set; }
    public decimal RevenueAll { get; set; }
    public IList<Order> RecentOrders { get; set; } = new List<Order>();
    public IList<Product> TopProducts { get; set; } = new List<Product>();

    // Dữ liệu cho Chart.js
    public IList<string> RevenueLabels { get; set; } = new List<string>();
    public IList<decimal> RevenueSeries { get; set; } = new List<decimal>();
    public IList<int> OrderSeries { get; set; } = new List<int>();
    public IList<string> CategoryLabels { get; set; } = new List<string>();
    public IList<int> CategoryProductCounts { get; set; } = new List<int>();
}
