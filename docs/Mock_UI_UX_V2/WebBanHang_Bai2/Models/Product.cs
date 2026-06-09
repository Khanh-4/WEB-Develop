using System.ComponentModel.DataAnnotations;

namespace WebBanHang_Bai2.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(150)]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Slug")]
    public string? Slug { get; set; }

    [Range(0.01, 1_000_000_000, ErrorMessage = "Giá phải lớn hơn 0")]
    [Display(Name = "Giá hiện tại")]
    public decimal Price { get; set; }

    [Display(Name = "Giá gốc")]
    public decimal? OldPrice { get; set; }

    [Display(Name = "Mô tả ngắn")]
    [StringLength(280)]
    public string? ShortDescription { get; set; }

    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }

    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Tên danh mục")]
    public string? Category { get; set; }

    [Display(Name = "Ảnh đại diện")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Bộ sưu tập ảnh")]
    public List<string>? ImageUrls { get; set; }

    [Display(Name = "Đánh giá TB")]
    [Range(0, 5)]
    public double Rating { get; set; }

    [Display(Name = "Lượt đánh giá")]
    public int ReviewCount { get; set; }

    [Display(Name = "Tồn kho")]
    public int Stock { get; set; } = 100;

    [Display(Name = "Đã bán")]
    public int Sold { get; set; }

    [Display(Name = "Sản phẩm HOT")]
    public bool IsHot { get; set; }

    [Display(Name = "Sản phẩm MỚI")]
    public bool IsNew { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Phần trăm giảm giá nếu có giá gốc.</summary>
    public int DiscountPercent =>
        OldPrice.HasValue && OldPrice.Value > Price
            ? (int)Math.Round((OldPrice.Value - Price) / OldPrice.Value * 100)
            : 0;
}
