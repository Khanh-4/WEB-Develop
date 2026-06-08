using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TechSpecs.Data;
using TechSpecs.ViewModels;

namespace TechSpecs.ViewComponents;

public class FeaturedCategoryViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public FeaturedCategoryViewComponent(AppDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IViewComponentResult> InvokeAsync(string category, string title, int count = 10)
    {
        var cacheKey = $"home-cat-{category}-{count}";
        var items = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return await FetchAsync(category, count);
        }) ?? new List<ProductListItem>();

        ViewBag.Title = title;
        ViewBag.Category = category;
        return View(items);
    }

    private async Task<List<ProductListItem>> FetchAsync(string category, int count)
    {
        var items = await FetchItemsAsync(category, count);

        // Attach rating summaries in a single batch query
        if (items.Count > 0)
        {
            var ids = items.Select(x => x.Id).ToList();
            var ratings = await _db.ProductReviews.AsNoTracking()
                .Where(r => r.Category == category && ids.Contains(r.ComponentId))
                .GroupBy(r => r.ComponentId)
                .Select(g => new { Id = g.Key, Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .ToDictionaryAsync(x => x.Id);

            foreach (var item in items)
                if (ratings.TryGetValue(item.Id, out var r))
                { item.AvgRating = Math.Round(r.Avg * 2) / 2; item.ReviewCount = r.Count; }
        }

        return items;
    }

    private async Task<List<ProductListItem>> FetchItemsAsync(string category, int count) => category switch
    {
        "cpu" => (await _db.Cpus.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(count).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cpu", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
                Specs = new() { ["Socket"] = c.Socket, ["Nhân/Luồng"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["TDP"] = $"{c.TDP}W" }
            }).ToList(),

        "gpu" => (await _db.VideoCards.AsNoTracking()
            .Where(g => g.Price > 0 && g.ImageUrl != null)
            .OrderByDescending(g => g.Price).Take(count).ToListAsync())
            .Select(g => new ProductListItem
            {
                Id = g.Id, Category = "gpu", Name = g.Name,
                Manufacturer = g.Manufacturer, Price = g.Price, ImageUrl = g.ImageUrl, Stock = g.Stock,
                Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["TDP"] = $"{g.TDP}W" }
            }).ToList(),

        "memory" => (await _db.Memories.AsNoTracking()
            .Where(m => m.Price > 0 && m.ImageUrl != null)
            .OrderByDescending(m => m.Price).Take(count).ToListAsync())
            .Select(m => new ProductListItem
            {
                Id = m.Id, Category = "memory", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl, Stock = m.Stock,
                Specs = new() { ["Loại"] = m.Type, ["Dung lượng"] = $"{m.Capacity}GB", ["Tốc độ"] = $"{m.Speed}MHz" }
            }).ToList(),

        "motherboard" => (await _db.Motherboards.AsNoTracking()
            .Where(m => m.Price > 0 && m.ImageUrl != null)
            .OrderByDescending(m => m.Price).Take(count).ToListAsync())
            .Select(m => new ProductListItem
            {
                Id = m.Id, Category = "motherboard", Name = m.Name,
                Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl, Stock = m.Stock,
                Specs = new() { ["Socket"] = m.SocketCompatibility, ["Form"] = m.FormFactor, ["RAM"] = m.MemoryCompatibility }
            }).ToList(),

        "storage" => (await _db.Storages.AsNoTracking()
            .Where(s => s.Price > 0 && s.ImageUrl != null)
            .OrderByDescending(s => s.Price).Take(count).ToListAsync())
            .Select(s => new ProductListItem
            {
                Id = s.Id, Category = "storage", Name = s.Name,
                Manufacturer = s.Manufacturer, Price = s.Price, ImageUrl = s.ImageUrl, Stock = s.Stock,
                Specs = new() { ["Loại"] = s.Type, ["Dung lượng"] = $"{s.Capacity}GB", ["Interface"] = s.Interface }
            }).ToList(),

        "psu" => (await _db.PowerSupplies.AsNoTracking()
            .Where(p => p.Price > 0 && p.ImageUrl != null)
            .OrderByDescending(p => p.Price).Take(count).ToListAsync())
            .Select(p => new ProductListItem
            {
                Id = p.Id, Category = "psu", Name = p.Name,
                Manufacturer = p.Manufacturer, Price = p.Price, ImageUrl = p.ImageUrl, Stock = p.Stock,
                Specs = new() { ["Công suất"] = $"{p.Wattage}W", ["Chuẩn"] = p.Efficiency }
            }).ToList(),

        "case" => (await _db.CaseEnclosures.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(count).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "case", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
                Specs = new() { ["Form Factor"] = c.FormFactorSupport, ["Max GPU"] = $"{c.MaxVGALength}mm" }
            }).ToList(),

        "cooler" => (await _db.CpuCoolers.AsNoTracking()
            .Where(c => c.Price > 0 && c.ImageUrl != null)
            .OrderByDescending(c => c.Price).Take(count).ToListAsync())
            .Select(c => new ProductListItem
            {
                Id = c.Id, Category = "cooler", Name = c.Name,
                Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl, Stock = c.Stock,
                Specs = new() { ["Loại"] = c.Type, ["Max TDP"] = $"{c.MaxTDP}W" }
            }).ToList(),

        _ => new List<ProductListItem>()
    };
}
