using System.ComponentModel.DataAnnotations;
using TechSpecs.Models;

namespace TechSpecs.ViewModels;

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.Subtotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public class CartItemViewModel
{
    public int CartItemId { get; set; }
    public string Category { get; set; } = string.Empty;
    public int ComponentId { get; set; }
    public string ComponentName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Subtotal => Price * Quantity;
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [MaxLength(200)]
    [Display(Name = "Họ và tên")]
    public string RecipientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [MaxLength(20)]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [MaxLength(500)]
    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Ghi chú (tuỳ chọn)")]
    public string? Note { get; set; }

    // For display
    public CartViewModel Cart { get; set; } = new();
}
