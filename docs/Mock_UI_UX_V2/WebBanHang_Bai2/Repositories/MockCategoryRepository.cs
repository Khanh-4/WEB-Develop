using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class MockCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories;

    public MockCategoryRepository()
    {
        _categories = new List<Category>
        {
            new() { Id = 1, Name = "Laptop", Icon = "bi-laptop", Description = "Laptop văn phòng, gaming, đồ hoạ" },
            new() { Id = 2, Name = "Desktop", Icon = "bi-pc-display", Description = "Máy tính để bàn & All-in-One" },
            new() { Id = 3, Name = "Phụ kiện", Icon = "bi-mouse2", Description = "Chuột, bàn phím, tai nghe, webcam" },
            new() { Id = 4, Name = "Màn hình", Icon = "bi-display", Description = "Màn hình gaming, đồ hoạ, văn phòng" }
        };
    }

    public IEnumerable<Category> GetAllCategories() => _categories;
    public Category? GetById(int id) => _categories.FirstOrDefault(c => c.Id == id);

    public void Add(Category category)
    {
        category.Id = _categories.Count == 0 ? 1 : _categories.Max(c => c.Id) + 1;
        _categories.Add(category);
    }

    public void Update(Category category)
    {
        var idx = _categories.FindIndex(c => c.Id == category.Id);
        if (idx >= 0) _categories[idx] = category;
    }

    public void Delete(int id) => _categories.RemoveAll(c => c.Id == id);
}
