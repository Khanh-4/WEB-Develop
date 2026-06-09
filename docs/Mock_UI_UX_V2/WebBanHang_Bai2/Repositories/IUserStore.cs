using System.Security.Cryptography;
using System.Text;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public interface IUserStore
{
    IEnumerable<AppUser> GetAll();
    AppUser? FindByName(string userName);
    AppUser? FindById(int id);
    (bool ok, string? error) Register(RegisterViewModel vm);
    AppUser? ValidateCredentials(string userName, string password);
    void Delete(int id);
}

public class MockUserStore : IUserStore
{
    private readonly List<AppUser> _users;

    public MockUserStore()
    {
        _users = new List<AppUser>
        {
            new() { Id = 1, UserName = "admin", Email = "admin@techstore.local", FullName = "Quản trị viên",
                    Role = "Admin", PasswordHash = Hash("admin123") },
            new() { Id = 2, UserName = "khachhang", Email = "kh@techstore.local", FullName = "Nguyễn Khách Hàng",
                    Role = "Customer", PasswordHash = Hash("123456"),
                    Phone = "0901234567", Address = "1 Nguyễn Huệ, Q.1, TP.HCM" },
            new() { Id = 3, UserName = "minhtuan", Email = "minhtuan@example.com", FullName = "Minh Tuấn",
                    Role = "Customer", PasswordHash = Hash("123456") },
        };
    }

    private static string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    public IEnumerable<AppUser> GetAll() => _users.OrderByDescending(u => u.CreatedAt);

    public AppUser? FindByName(string userName) =>
        _users.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));

    public AppUser? FindById(int id) => _users.FirstOrDefault(u => u.Id == id);

    public (bool ok, string? error) Register(RegisterViewModel vm)
    {
        if (FindByName(vm.UserName) is not null)
            return (false, "Tên đăng nhập đã tồn tại.");
        if (_users.Any(u => string.Equals(u.Email, vm.Email, StringComparison.OrdinalIgnoreCase)))
            return (false, "Email đã được sử dụng.");

        var user = new AppUser
        {
            Id = _users.Count == 0 ? 1 : _users.Max(u => u.Id) + 1,
            UserName = vm.UserName,
            Email = vm.Email,
            FullName = vm.FullName,
            PasswordHash = Hash(vm.Password),
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        _users.Add(user);
        return (true, null);
    }

    public AppUser? ValidateCredentials(string userName, string password)
    {
        var user = FindByName(userName);
        if (user is null) return null;
        return user.PasswordHash == Hash(password) ? user : null;
    }

    public void Delete(int id) => _users.RemoveAll(u => u.Id == id && u.UserName != "admin");
}
