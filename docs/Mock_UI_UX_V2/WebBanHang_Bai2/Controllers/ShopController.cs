using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Controllers;

public class ShopController : Controller
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;

    public ShopController(IProductRepository products, ICategoryRepository categories)
    {
        _products = products;
        _categories = categories;
    }

    public IActionResult Index(int? categoryId, string? keyword, string sort = "newest",
        decimal? minPrice = null, decimal? maxPrice = null, int page = 1, int pageSize = 12)
    {
        var query = _products.GetAll().AsEnumerable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLowerInvariant().Contains(kw)
                || (p.Category ?? "").ToLowerInvariant().Contains(kw)
                || (p.ShortDescription ?? "").ToLowerInvariant().Contains(kw));
        }

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "rating" => query.OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewCount),
            "hot" => query.OrderByDescending(p => p.Sold),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = query.Count();
        page = Math.Max(1, page);
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var vm = new ShopViewModel
        {
            Products = items,
            Categories = _categories.GetAllCategories().ToList(),
            CategoryId = categoryId,
            Keyword = keyword,
            Sort = sort,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };

        return View(vm);
    }
}
