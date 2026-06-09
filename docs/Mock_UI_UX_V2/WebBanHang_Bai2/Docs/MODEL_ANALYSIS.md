# MODEL ANALYSIS — TechStore

> Source: toàn bộ files trong `Models/`

Model trong ASP.NET Core MVC là các C# class đại diện cho dữ liệu. Có hai loại:
- **Domain Model / Entity:** Map 1-1 với bảng database (Product, Category, Order, Review, ApplicationUser)
- **ViewModel:** Không lưu DB, chỉ dùng để truyền data giữa Controller và View (ShopViewModel, CheckoutViewModel, LoginViewModel, RegisterViewModel)

---

## 1. Product (`Models/Product.cs`)

**Mục đích:** Đại diện cho một sản phẩm trong cửa hàng. Map tới bảng `Products` trong PostgreSQL.

| Thuộc tính | Kiểu | Annotation | Ý nghĩa |
|---|---|---|---|
| `Id` | `int` | — | Khóa chính, EF Core tự tăng |
| `Name` | `string` | `[Required]`, `[StringLength(150)]` | Tên sản phẩm, bắt buộc, tối đa 150 ký tự |
| `Slug` | `string?` | `[Display]` | URL slug, ví dụ: `laptop-dell-xps-13-plus` |
| `Price` | `decimal` | `[Range(0.01, 1_000_000_000)]` | Giá hiện tại, phải > 0 |
| `OldPrice` | `decimal?` | `[Display]` | Giá gốc (nullable — null = không giảm giá) |
| `ShortDescription` | `string?` | `[StringLength(280)]` | Mô tả ngắn, hiển thị trên card sản phẩm |
| `Description` | `string?` | — | Mô tả chi tiết đầy đủ |
| `CategoryId` | `int` | `[Display]` | ID danh mục |
| `Category` | `string?` | `[Display]` | Tên danh mục (denormalized) |
| `ImageUrl` | `string?` | `[Display]` | Đường dẫn ảnh chính |
| `ImageUrls` | `List<string>?` | — | Danh sách ảnh phụ (lưu JSON trong DB) |
| `Rating` | `double` | `[Range(0, 5)]` | Điểm trung bình (tự cập nhật khi có review mới) |
| `ReviewCount` | `int` | — | Số lượng review |
| `Stock` | `int` | — | Tồn kho, mặc định 100 |
| `Sold` | `int` | — | Số đã bán |
| `IsHot` | `bool` | — | True = hiển thị badge HOT |
| `IsNew` | `bool` | — | True = hiển thị badge NEW |
| `CreatedAt` | `DateTime` | — | Ngày tạo, mặc định `DateTime.UtcNow` |
| `DiscountPercent` | `int` (computed) | — | Tính từ OldPrice và Price, **không lưu DB** |

**Computed Property:**
```csharp
// Models/Product.cs:66
public int DiscountPercent =>
    OldPrice.HasValue && OldPrice.Value > Price
        ? (int)Math.Round((OldPrice.Value - Price) / OldPrice.Value * 100)
        : 0;
```
EF Core bỏ qua property này (`Ignore`) — chỉ tính trên C# khi cần hiển thị.

**Ví dụ dữ liệu thực tế (từ DbSeeder.cs):**
```
Name: "MacBook Air M2 13""
Price: 28_500_000
OldPrice: 32_000_000
DiscountPercent: 10 (tính tự động: (32M-28.5M)/32M * 100 ≈ 10%)
Rating: 4.9
ReviewCount: 287
Sold: 540
IsHot: true, IsNew: true
ImageUrls: ["/images/laptop-mac-2.svg", "/images/laptop-mac-3.svg"]
```

---

## 2. Category (`Models/Category.cs`)

**Mục đích:** Danh mục phân loại sản phẩm. Map tới bảng `Categories`.

| Thuộc tính | Kiểu | Annotation | Ý nghĩa |
|---|---|---|---|
| `Id` | `int` | — | Khóa chính |
| `Name` | `string` | `[Required]`, `[StringLength(50)]` | Tên danh mục, bắt buộc |
| `Icon` | `string` | — | Bootstrap Icons class, mặc định `"bi-tag"` |
| `Description` | `string?` | `[StringLength(160)]` | Mô tả ngắn |

**Ví dụ dữ liệu:**
```
{ Id=1, Name="Laptop", Icon="bi-laptop", Description="Laptop văn phòng, gaming, đồ hoạ" }
{ Id=3, Name="Phụ kiện", Icon="bi-mouse2", Description="Chuột, bàn phím, tai nghe, webcam" }
```

**Cách dùng Icon:** Trong View, render `<i class="bi @c.Icon"></i>` để hiển thị icon tương ứng.

---

## 3. ApplicationUser (`Models/ApplicationUser.cs`)

**Mục đích:** Tài khoản người dùng. Kế thừa `IdentityUser` (ASP.NET Core Identity) và thêm các field tùy chỉnh.

| Thuộc tính | Kiểu | Annotation | Ý nghĩa |
|---|---|---|---|
| `FullName` | `string` | `[Required]`, `[StringLength(120)]` | Họ tên đầy đủ (custom field) |
| `Phone` | `string?` | — | Số điện thoại (custom field) |
| `Address` | `string?` | — | Địa chỉ giao hàng mặc định (custom field) |
| `AvatarUrl` | `string?` | — | URL ảnh đại diện (custom field) |
| `CreatedAt` | `DateTime` | — | Ngày đăng ký (custom field) |
| `Role` | `string` | `[NotMapped]` | Role hiện tại, **không lưu DB**, populate bởi controller |

**Các field kế thừa từ IdentityUser (quan trọng):**
- `Id` — GUID string, khóa chính
- `UserName` — tên đăng nhập
- `Email` — email
- `PasswordHash` — mật khẩu đã hash (PBKDF2)
- `SecurityStamp` — thay đổi khi đổi password/role → vô hiệu cookie cũ

**`[NotMapped]` là gì?** Annotation này báo EF Core bỏ qua field `Role` khi tạo/cập nhật database. Field này chỉ tồn tại trong RAM (C# object), không có cột tương ứng trong DB. Controllers cần tự fill:
```csharp
// AccountController.cs:106
user.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";
```

---

## 4. Order (`Models/Order.cs`)

**Mục đích:** Đơn hàng sau khi khách checkout. Map tới bảng `Orders`.

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | `int` | Khóa chính |
| `OrderCode` | `string` | Mã đơn hàng, unique. Format: `ORD20260605130045123` |
| `CustomerName` | `string` | Tên người nhận |
| `Email` | `string` | Email khách |
| `Phone` | `string` | SĐT khách |
| `ShippingAddress` | `string` | Địa chỉ giao hàng |
| `Notes` | `string?` | Ghi chú của khách |
| `PaymentMethod` | `string` | COD / VNPay / Momo / BankTransfer |
| `Subtotal` | `decimal` | Tổng tiền hàng |
| `ShippingFee` | `decimal` | Phí ship (0 nếu Subtotal >= 500K) |
| `Total` | `decimal` | Tổng = Subtotal + ShippingFee |
| `Status` | `OrderStatus` | Enum trạng thái đơn hàng |
| `CreatedAt` | `DateTime` | Thời điểm đặt |
| `UserName` | `string?` | UserName người đặt (null nếu đặt ẩn danh) |
| `Items` | `List<OrderDetail>` | Danh sách chi tiết đơn hàng |

**Enum OrderStatus:**
```csharp
Pending = 0    // Chờ xác nhận (mặc định khi đặt)
Confirmed = 1  // Admin xác nhận
Shipping = 2   // Đang vận chuyển
Completed = 3  // Đã giao thành công
Cancelled = 4  // Đã huỷ
```

**Cấu hình EF Core (ApplicationDbContext.cs:40):**
```csharp
builder.Entity<Order>().OwnsMany(o => o.Items, od => {
    od.WithOwner().HasForeignKey("OrderId");
    od.ToTable("OrderDetails");
    od.Ignore(x => x.LineTotal);
});
```
`OwnsMany` = OrderDetail là "owned entity" — hoàn toàn phụ thuộc vào Order, không có DbSet riêng.

---

## 5. OrderDetail (`Models/Order.cs:35`)

**Mục đích:** Một dòng sản phẩm trong đơn hàng. Map tới bảng `OrderDetails`.

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `ProductId` | `int` | ID sản phẩm (snapshot, không FK) |
| `ProductName` | `string` | Tên sản phẩm tại lúc đặt |
| `ImageUrl` | `string?` | Ảnh sản phẩm tại lúc đặt |
| `Price` | `decimal` | Giá tại lúc đặt (snapshot) |
| `Quantity` | `int` | Số lượng |
| `LineTotal` | `decimal` (computed) | = Price × Quantity, không lưu DB |

---

## 6. CheckoutViewModel (`Models/Order.cs:44`)

**Mục đích:** ViewModel cho form checkout. Không lưu database — dùng để validate input và tạo Order.

| Thuộc tính | Annotation | Ý nghĩa |
|---|---|---|
| `CustomerName` | `[Required]` | Tên người nhận, bắt buộc |
| `Email` | `[Required]`, `[EmailAddress]` | Email hợp lệ |
| `Phone` | `[Required]`, `[Phone]` | SĐT hợp lệ |
| `ShippingAddress` | `[Required]` | Địa chỉ giao hàng, bắt buộc |
| `Notes` | — | Ghi chú (tùy chọn) |
| `PaymentMethod` | `[Required]` | Phương thức thanh toán, mặc định "COD" |

Khi POST, Controller map CheckoutViewModel → tạo Order entity rồi lưu DB.

---

## 7. Review (`Models/Review.cs`)

**Mục đích:** Đánh giá sản phẩm của khách. Map tới bảng `Reviews`.

| Thuộc tính | Annotation | Ý nghĩa |
|---|---|---|
| `Id` | — | Khóa chính |
| `ProductId` | — | ID sản phẩm được đánh giá |
| `CustomerName` | `[Required]`, `[StringLength(80)]` | Tên người đánh giá |
| `Rating` | `[Range(1,5)]` | Số sao 1–5 |
| `Comment` | `[Required]`, `[StringLength(1000)]` | Nội dung, tối đa 1000 ký tự |
| `CreatedAt` | — | Thời điểm đánh giá |

Sau khi thêm review, `ProductController.Review` tự cập nhật `Product.Rating` và `Product.ReviewCount`:
```csharp
// ProductController.cs:50
p.Rating = Math.Round(all.Average(r => r.Rating), 1);
p.ReviewCount = all.Count;
_products.Update(p);
```

---

## 8. ShoppingCart và CartItem (`Models/Cart.cs`)

**Mục đích:** Giỏ hàng lưu trong Session (không lưu DB). Được serialize/deserialize qua JSON.

### CartItem

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `ProductId` | `int` | ID sản phẩm |
| `Name` | `string` | Tên sản phẩm |
| `Price` | `decimal` | Giá tại lúc thêm vào giỏ |
| `Quantity` | `int` | Số lượng |
| `ImageUrl` | `string?` | Ảnh sản phẩm |
| `LineTotal` | `decimal` (computed) | = Price × Quantity |

### ShoppingCart

| Thuộc tính | Ý nghĩa |
|---|---|
| `Items` | Danh sách CartItem |
| `TotalQuantity` | Tổng số lượng (hiển thị badge) |
| `Subtotal` | Tổng tiền hàng |
| `ShippingFee` | 0 nếu Subtotal >= 500K, ngược lại 30K |
| `Total` | Subtotal + ShippingFee |

**Các method:**
```csharp
Add(CartItem item)      // Nếu đã có → tăng quantity; nếu chưa → thêm mới
Update(int id, int qty) // qty <= 0 → xoá hẳn; qty > 0 → cập nhật
Remove(int id)          // Xoá item theo ProductId
Clear()                 // Xoá toàn bộ giỏ
```

---

## 9. ShopViewModel (`Models/ShopViewModel.cs`)

**Mục đích:** Truyền dữ liệu từ `ShopController` sang `Views/Shop/Index.cshtml`.

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `Products` | `IList<Product>` | Danh sách sản phẩm đã lọc/sắp xếp/phân trang |
| `Categories` | `IList<Category>` | Danh sách danh mục để render sidebar filter |
| `CategoryId` | `int?` | Danh mục đang lọc (null = tất cả) |
| `Keyword` | `string?` | Từ khóa tìm kiếm |
| `Sort` | `string` | Kiểu sắp xếp, mặc định "newest" |
| `MinPrice` | `decimal?` | Giá tối thiểu |
| `MaxPrice` | `decimal?` | Giá tối đa |
| `Page` | `int` | Trang hiện tại (mặc định 1) |
| `PageSize` | `int` | Số SP mỗi trang (mặc định 12) |
| `TotalItems` | `int` | Tổng số SP thỏa mãn điều kiện lọc |
| `TotalPages` | `int` (computed) | = Math.Ceiling(TotalItems / PageSize) |

---

## 10. ProductDetailViewModel (`Models/ShopViewModel.cs:19`)

**Mục đích:** Truyền dữ liệu cho trang chi tiết sản phẩm.

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `Product` | `Product` | Sản phẩm đang xem |
| `Related` | `IList<Product>` | Tối đa 4 SP cùng danh mục |
| `Reviews` | `IList<Review>` | Danh sách đánh giá của SP này |

---

## 11. DashboardViewModel (`Models/ShopViewModel.cs:25`)

**Mục đích:** Truyền dữ liệu thống kê cho Admin Dashboard.

| Thuộc tính | Kiểu | Ý nghĩa |
|---|---|---|
| `TotalProducts` | `int` | Tổng số sản phẩm |
| `TotalCategories` | `int` | Tổng số danh mục |
| `TotalOrders` | `int` | Tổng đơn hàng |
| `TotalUsers` | `int` | Tổng tài khoản |
| `OrdersToday` | `int` | Số đơn hôm nay |
| `RevenueMonth` | `decimal` | Doanh thu tháng này (bỏ Cancelled) |
| `RevenueAll` | `decimal` | Tổng doanh thu (bỏ Cancelled) |
| `RecentOrders` | `IList<Order>` | 8 đơn hàng gần nhất |
| `TopProducts` | `IList<Product>` | 5 SP bán chạy nhất |
| `RevenueLabels` | `IList<string>` | Label ngày cho Chart.js (14 ngày) |
| `RevenueSeries` | `IList<decimal>` | Doanh thu từng ngày |
| `OrderSeries` | `IList<int>` | Số đơn từng ngày |
| `CategoryLabels` | `IList<string>` | Tên danh mục cho biểu đồ tròn |
| `CategoryProductCounts` | `IList<int>` | Số SP theo từng danh mục |

---

## 12. LoginViewModel (`Models/LoginViewModel.cs`)

**Mục đích:** Model cho form đăng nhập.

| Thuộc tính | Annotation | Ý nghĩa |
|---|---|---|
| `UserName` | `[Required]` | Tên đăng nhập |
| `Password` | `[Required]`, `[DataType(DataType.Password)]` | Mật khẩu (render input type=password) |
| `RememberMe` | — | Ghi nhớ đăng nhập (persistent cookie) |

---

## 13. RegisterViewModel (`Models/AppUser.cs:31`)

**Mục đích:** Model cho form đăng ký tài khoản.

| Thuộc tính | Annotation | Ý nghĩa |
|---|---|---|
| `FullName` | `[Required]` | Họ tên đầy đủ |
| `UserName` | `[Required]`, `[StringLength(64, MinimumLength=3)]` | Tên đăng nhập, 3–64 ký tự |
| `Email` | `[Required]`, `[EmailAddress]` | Email hợp lệ |
| `Password` | `[Required]`, `[StringLength(100, MinimumLength=6)]` | Mật khẩu, tối thiểu 6 ký tự |
| `ConfirmPassword` | `[Compare(nameof(Password))]` | Phải khớp với Password |

**`[Compare]` annotation:** Server-side validation tự động so sánh ConfirmPassword với Password. Nếu không khớp → `ModelState.IsValid = false`.

---

## 14. AppUser (`Models/AppUser.cs:7`)

**Mục đích:** Model tài khoản dùng cho mock (in-memory) — legacy từ giai đoạn đầu phát triển trước khi có Identity. Hiện tại **không được sử dụng** trong logic chính, không inject vào DI container. Chỉ còn lại trong codebase vì lý do lịch sử.

---

## Tổng hợp — Phân loại Models

```
Domain Models (lưu DB):
├── Product           → bảng Products
├── Category          → bảng Categories
├── ApplicationUser   → bảng AspNetUsers
├── Order             → bảng Orders
├── OrderDetail       → bảng OrderDetails (owned by Order)
└── Review            → bảng Reviews

Session Models (không lưu DB):
├── ShoppingCart      → Session key "TechStore.Cart"
└── CartItem          → lưu trong ShoppingCart.Items

ViewModels (không lưu DB, chỉ truyền View):
├── ShopViewModel        → Shop/Index.cshtml
├── ProductDetailViewModel → Product/Detail.cshtml
├── DashboardViewModel   → Areas/Admin/Views/Dashboard/Index.cshtml
├── CheckoutViewModel    → Checkout/Index.cshtml (form input)
├── LoginViewModel       → Account/Login.cshtml
└── RegisterViewModel    → Account/Register.cshtml

Legacy (không dùng):
└── AppUser           → Models/AppUser.cs
```
