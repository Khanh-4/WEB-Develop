using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public interface IReviewRepository
{
    IEnumerable<Review> GetAll();
    IEnumerable<Review> GetByProduct(int productId);
    void Add(Review review);
    void Delete(int id);
}

public class MockReviewRepository : IReviewRepository
{
    private readonly List<Review> _reviews;

    public MockReviewRepository()
    {
        _reviews = new List<Review>
        {
            new() { Id = 1, ProductId = 1, CustomerName = "Minh Tuấn", Rating = 5, Comment = "Dell XPS 13 xứng đáng số tiền bỏ ra. Màn hình OLED quá đẹp, build chắc chắn.", CreatedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = 2, ProductId = 1, CustomerName = "Hồng Anh", Rating = 4, Comment = "Pin chỉ trung bình so với kỳ vọng, còn lại đều rất tốt.", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new() { Id = 3, ProductId = 2, CustomerName = "Đức Long", Rating = 5, Comment = "M2 mát hơn M1 nhiều, pin trâu. Quá hài lòng!", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 4, ProductId = 8, CustomerName = "Trang Nguyễn", Rating = 5, Comment = "MX Master 3S quá mượt, click êm, dùng work-from-home không thể tốt hơn.", CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new() { Id = 5, ProductId = 10, CustomerName = "Hải Đăng", Rating = 5, Comment = "Sony WH-1000XM5 chống ồn cực tốt, đeo lâu vẫn thoải mái.", CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 6, ProductId = 12, CustomerName = "Phương Linh", Rating = 4, Comment = "Màn hình màu chuẩn, 165Hz mượt mà. Stand hơi rung khi đẩy bàn.", CreatedAt = DateTime.UtcNow.AddDays(-1) },
        };
    }

    public IEnumerable<Review> GetAll() => _reviews.OrderByDescending(r => r.CreatedAt);
    public IEnumerable<Review> GetByProduct(int productId) =>
        _reviews.Where(r => r.ProductId == productId).OrderByDescending(r => r.CreatedAt);

    public void Add(Review review)
    {
        review.Id = _reviews.Count == 0 ? 1 : _reviews.Max(r => r.Id) + 1;
        review.CreatedAt = DateTime.UtcNow;
        _reviews.Add(review);
    }

    public void Delete(int id) => _reviews.RemoveAll(r => r.Id == id);
}
