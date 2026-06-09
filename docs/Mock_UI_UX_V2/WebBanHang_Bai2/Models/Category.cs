using System.ComponentModel.DataAnnotations;

namespace WebBanHang_Bai2.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Icon (Bootstrap Icons class)")]
    public string Icon { get; set; } = "bi-tag";

    [Display(Name = "Mô tả ngắn")]
    [StringLength(160)]
    public string? Description { get; set; }
}
