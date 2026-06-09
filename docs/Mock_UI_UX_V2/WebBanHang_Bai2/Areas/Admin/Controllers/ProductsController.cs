using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly IProductRepository _products;
    private readonly ICategoryRepository _categories;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductRepository products, ICategoryRepository categories, IWebHostEnvironment env)
    {
        _products = products;
        _categories = categories;
        _env = env;
    }

    public IActionResult Index(string? keyword, int? categoryId, string sort = "newest", int page = 1, int pageSize = 10)
    {
        var query = _products.GetAll().AsEnumerable();
        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLowerInvariant().Contains(kw));
        }

        query = sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name_asc" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        ViewBag.Categories = _categories.GetAllCategories().ToList();
        ViewBag.CategoryId = categoryId;
        ViewBag.Keyword = keyword;
        ViewBag.Sort = sort;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = total;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        return View(items);
    }

    [HttpGet]
    public IActionResult Add()
    {
        PopulateCategories();
        return View(new Product { Stock = 100 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Add(Product product, IFormFile? mainImage, List<IFormFile>? newImages)
    {
        if (!ModelState.IsValid)
        {
            PopulateCategories(product.CategoryId);
            return View(product);
        }

        if (mainImage is { Length: > 0 })
            product.ImageUrl = await SaveImage(mainImage);

        product.ImageUrls = await SaveManyAsync(newImages);
        product.Category = _categories.GetById(product.CategoryId)?.Name;
        _products.Add(product);

        TempData["Success"] = $"Đã thêm sản phẩm \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Update(int id)
    {
        var p = _products.GetById(id);
        if (p is null) return NotFound();
        PopulateCategories(p.CategoryId);
        return View(p);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> Update(Product product, IFormFile? mainImage, List<IFormFile>? newImages)
    {
        if (!ModelState.IsValid)
        {
            PopulateCategories(product.CategoryId);
            return View(product);
        }
        var existing = _products.GetById(product.Id);
        if (existing is null) return NotFound();

        if (mainImage is { Length: > 0 })
            product.ImageUrl = await SaveImage(mainImage);
        else if (string.IsNullOrEmpty(product.ImageUrl))
            product.ImageUrl = existing.ImageUrl;

        product.ImageUrls ??= new List<string>();
        var added = await SaveManyAsync(newImages);
        product.ImageUrls.AddRange(added);

        product.Category = _categories.GetById(product.CategoryId)?.Name;
        product.Rating = existing.Rating;
        product.ReviewCount = existing.ReviewCount;
        product.Sold = existing.Sold;
        product.CreatedAt = existing.CreatedAt;
        _products.Update(product);

        TempData["Success"] = $"Đã cập nhật \"{product.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        var p = _products.GetById(id);
        _products.Delete(id);
        TempData["Success"] = p is null ? "Đã xoá." : $"Đã xoá \"{p.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateCategories(int? selected = null)
        => ViewBag.Categories = new SelectList(_categories.GetAllCategories(), "Id", "Name", selected);

    private async Task<List<string>> SaveManyAsync(List<IFormFile>? files)
    {
        var saved = new List<string>();
        if (files is null) return saved;
        foreach (var f in files)
            if (f.Length > 0) saved.Add(await SaveImage(f));
        return saved;
    }

    private async Task<string> SaveImage(IFormFile image)
    {
        var dir = Path.Combine(_env.WebRootPath, "images");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName)}";
        var path = Path.Combine(dir, fileName);
        await using var fs = new FileStream(path, FileMode.Create);
        await image.CopyToAsync(fs);
        return $"/images/{fileName}";
    }
}
