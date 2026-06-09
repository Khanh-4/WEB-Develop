using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public interface ICategoryRepository
{
    IEnumerable<Category> GetAllCategories();
    Category? GetById(int id);
    void Add(Category category);
    void Update(Category category);
    void Delete(int id);
}
