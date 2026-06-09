using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebBanHang_Bai2.Models;

public class ApplicationUser : IdentityUser
{
    [Required]
    [StringLength(120)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Non-mapped helper — populated by controllers when needed for views
    [NotMapped]
    public string Role { get; set; } = "Customer";
}
