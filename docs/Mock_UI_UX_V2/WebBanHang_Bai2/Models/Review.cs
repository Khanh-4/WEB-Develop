using System.ComponentModel.DataAnnotations;

namespace WebBanHang_Bai2.Models;

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên")]
    [StringLength(80)]
    [Display(Name = "Tên hiển thị")]
    public string CustomerName { get; set; } = string.Empty;

    [Range(1, 5)]
    [Display(Name = "Số sao")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
    [StringLength(1000)]
    [Display(Name = "Nội dung đánh giá")]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
