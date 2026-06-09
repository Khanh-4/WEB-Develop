using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ICategoryRepository _categories;
    private readonly IProductRepository _products;

    public CategoriesController(ICategoryRepository categories, IProductRepository products)
    {
        _categories = categories;
        _products = products;
    }

    public IActionResult Index()
    {
        var prods = _products.GetAll().ToList();
        ViewBag.ProductCounts = _categories.GetAllCategories()
            .ToDictionary(c => c.Id, c => prods.Count(p => p.CategoryId == c.Id));
        return View(_categories.GetAllCategories().ToList());
    }

    [HttpGet] public IActionResult Add() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        _categories.Add(category);
        TempData["Success"] = "Đã thêm danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Update(int id)
    {
        var c = _categories.GetById(id);
        return c is null ? NotFound() : View(c);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(Category category)
    {
        if (!ModelState.IsValid) return View(category);
        _categories.Update(category);
        TempData["Success"] = "Đã cập nhật danh mục.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        _categories.Delete(id);
        TempData["Success"] = "Đã xoá danh mục.";
        return RedirectToAction(nameof(Index));
    }
}
