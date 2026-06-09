using System.ComponentModel.DataAnnotations;

namespace WebBanHang_Bai2.Models;

/// <summary>Tài khoản người dùng — lưu trữ in-memory (mock).</summary>
public class AppUser
{
    public int Id { get; set; }

    [Required, StringLength(64)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Customer";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    [StringLength(64, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp")]
    [Display(Name = "Xác nhận mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
