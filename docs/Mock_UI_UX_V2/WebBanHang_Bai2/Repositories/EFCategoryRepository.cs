using WebBanHang_Bai2.Data;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class EFCategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public EFCategoryRepository(ApplicationDbContext context) => _context = context;

    public IEnumerable<Category> GetAllCategories() => _context.Categories.ToList();

    public Category? GetById(int id) => _context.Categories.Find(id);

    public void Add(Category category)
    {
        _context.Categories.Add(category);
        _context.SaveChanges();
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var c = _context.Categories.Find(id);
        if (c is not null)
        {
            _context.Categories.Remove(c);
            _context.SaveChanges();
        }
    }
}
