using System.ComponentModel.DataAnnotations;

namespace WebBanHang_Bai2.Models;

public enum OrderStatus
{
    Pending = 0,        // Chờ xác nhận
    Confirmed = 1,      // Đã xác nhận
    Shipping = 2,       // Đang giao
    Completed = 3,      // Hoàn tất
    Cancelled = 4       // Đã huỷ
}

public class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string PaymentMethod { get; set; } = "COD";
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? UserName { get; set; }

    public List<OrderDetail> Items { get; set; } = new();
}

public class OrderDetail
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal => Price * Quantity;
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
    [Display(Name = "Họ và tên")]
    public string CustomerName { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
    [Display(Name = "Địa chỉ giao hàng")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Display(Name = "Ghi chú")]
    public string? Notes { get; set; }

    [Required]
    [Display(Name = "Phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "COD";
}
