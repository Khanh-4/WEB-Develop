using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Data;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class EFReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _context;

    public EFReviewRepository(ApplicationDbContext context) => _context = context;

    public IEnumerable<Review> GetAll() =>
        _context.Reviews.OrderByDescending(r => r.CreatedAt).ToList();

    public IEnumerable<Review> GetByProduct(int productId) =>
        _context.Reviews.Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt).ToList();

    public void Add(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        _context.SaveChanges();
    }

    public void Delete(int id)
    {
        var r = _context.Reviews.Find(id);
        if (r is not null)
        {
            _context.Reviews.Remove(r);
            _context.SaveChanges();
        }
    }
}
