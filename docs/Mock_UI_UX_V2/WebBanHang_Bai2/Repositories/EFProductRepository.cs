using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Data;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class EFProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public EFProductRepository(ApplicationDbContext context) => _context = context;

    public IEnumerable<Product> GetAll() => _context.Products.ToList();

    public Product? GetById(int id) => _context.Products.Find(id);

    public void Add(Product product)
    {
        if (string.IsNullOrEmpty(product.Slug))
            product.Slug = Slugify(product.Name);
        product.CreatedAt = DateTime.UtcNow;
        product.IsNew = true;
        _context.Products.Add(product);
        _context.SaveChanges();
    }

    public void Update(Product product)
    {
        product.Slug = Slugify(product.Name);
        _context.Products.Update(product);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var p = _context.Products.Find(id);
        if (p is not null)
        {
            _context.Products.Remove(p);
            _context.SaveChanges();
        }
    }

    private static string Slugify(string s) =>
        new string(s.ToLowerInvariant()
            .Replace('đ', 'd')
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
        .Trim('-');
}
