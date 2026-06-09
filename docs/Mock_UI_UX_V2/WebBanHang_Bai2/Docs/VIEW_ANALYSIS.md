# VIEW ANALYSIS — TechStore

> Source: toàn bộ files trong `Views/` và `Areas/Admin/Views/`

Views trong ASP.NET Core MVC là các file `.cshtml` (Razor syntax) — kết hợp HTML với C# code để render giao diện người dùng. View nhận dữ liệu từ Controller qua `@model` (strongly typed) hoặc `ViewBag`/`ViewData` (dynamic).

---

## Cấu trúc Views tổng quan

```
Views/
├── Shared/
│   ├── _Layout.cshtml          # Layout master — dùng cho tất cả customer views
│   ├── _ProductCard.cshtml     # Partial view: card sản phẩm
│   ├── _ValidationScriptsPartial.cshtml  # Script validate client-side
│   └── Error.cshtml            # Trang lỗi chung
├── Account/
│   ├── Login.cshtml            # Form đăng nhập
│   ├── Register.cshtml         # Form đăng ký
│   ├── Profile.cshtml          # Trang thông tin cá nhân
│   ├── Orders.cshtml           # Lịch sử đơn hàng
│   └── AccessDenied.cshtml     # Trang bị từ chối
├── Cart/Index.cshtml           # Trang giỏ hàng
├── Checkout/
│   ├── Index.cshtml            # Form thanh toán
│   └── Success.cshtml          # Đặt hàng thành công
├── Home/
│   ├── Index.cshtml            # Trang chủ
│   ├── About.cshtml            # Giới thiệu
│   ├── Contact.cshtml          # Liên hệ
│   └── Privacy.cshtml          # Chính sách
├── Product/Detail.cshtml       # Chi tiết sản phẩm
├── Shop/Index.cshtml           # Danh sách + filter
└── Wishlist/Index.cshtml       # Danh sách yêu thích

Areas/Admin/Views/
├── Shared/_AdminLayout.cshtml  # Layout riêng cho Admin
├── Dashboard/Index.cshtml      # Trang tổng quan
├── Products/
│   ├── Index.cshtml, Add.cshtml, Update.cshtml
├── Categories/
│   ├── Index.cshtml, Add.cshtml, Update.cshtml
├── Orders/Index.cshtml, Detail.cshtml
├── Reviews/Index.cshtml
└── Users/Index.cshtml
```

---

## 1. `Views/Shared/_Layout.cshtml`

**Mục đích:** Layout master — wrapper chứa header, nav, footer và cart drawer cho toàn bộ trang customer-facing.

**Injected services:**
```cshtml
@inject CartService CartSvc
@inject WishlistService WishlistSvc
@inject ICategoryRepository CatRepo
```
Layout trực tiếp inject service — không cần Controller truyền qua ViewBag. Đây là cách ASP.NET Core cho phép partial views và layouts truy cập DI container.

**Cấu trúc HTML chính:**
- `<head>`: Bootstrap 5, Bootstrap Icons 1.11.3, Google Fonts Inter, site.css
- `.top-bar`: Thông báo freeship, hotline
- `.main-bar`: Logo TechStore, thanh search (form GET tới Shop/Index), icons (wishlist, cart, user dropdown)
- `.tx-nav`: Navigation bar với mega menu danh mục (render động từ `CatRepo`)
- `<main>`: `@RenderBody()` — nội dung từng trang
- `<footer>`: Links, newsletter form
- **Cart Drawer** (sidebar): Panel ẩn mặc định, hiện khi click icon giỏ hàng, load nội dung qua AJAX

**Authentication-aware menu:**
```cshtml
@if (User.Identity?.IsAuthenticated == true)
{
    // Hiển thị avatar + dropdown (Profile, Đơn hàng, Admin Dashboard nếu là Admin)
}
else
{
    // Hiển thị nút Đăng nhập
}
```

**Alert từ TempData:**
```cshtml
@if (TempData["Success"] is string ok)  { <div class="alert alert-success">...</div> }
@if (TempData["Error"] is string err)   { <div class="alert alert-danger">...</div> }
```

**Scripts cuối trang:** jQuery, Bootstrap bundle, site.js. Layouts render `@await RenderSectionAsync("Scripts", required: false)` — cho phép trang con thêm scripts riêng.

---

## 2. `Views/Shared/_ProductCard.cshtml`

**Mục đích:** Partial view hiển thị card sản phẩm. Được dùng lại tại Home/Index, Shop/Index, Product/Detail (related products).

**Model:** `@model WebBanHang_Bai2.Models.Product`

**Nội dung:**
- Ảnh sản phẩm với badges HOT/NEW/Discount
- Tên sản phẩm (link đến Product/Detail/{id})
- Đánh giá sao (render từ `Product.Rating`)
- Giá hiện tại + giá gốc gạch ngang
- Button "Thêm vào giỏ" (AJAX) + Button "Yêu thích" (AJAX)

**Cách dùng:**
```cshtml
<partial name="_ProductCard" model="p" />
```

---

## 3. `Views/Account/Login.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.LoginViewModel`

**Mục đích:** Form đăng nhập 2 cột (cover marketing + form).

**Form:**
```html
<form asp-action="Login" asp-route-returnUrl="@ViewData["ReturnUrl"]" method="post">
```
- `asp-action`: POST đến `AccountController.Login`
- `asp-route-returnUrl`: Truyền returnUrl qua query string sau redirect

**Các field:**
| Field | Tag Helper | Validation |
|---|---|---|
| UserName | `<input asp-for="UserName">` | [Required] client + server |
| Password | `<input asp-for="Password">` | [Required], type=password |
| RememberMe | `<input asp-for="RememberMe">` | checkbox |

**Validation:**
- `<div asp-validation-summary="ModelOnly">` — hiển thị lỗi từ `ModelState.AddModelError("", ...)`
- `<partial name="_ValidationScriptsPartial" />` — load jQuery Validation cho client-side

**Tag Helper `asp-for`:** Tự động:
1. Set `name` attribute khớp với property name
2. Set `id` attribute
3. Đặt giá trị từ model
4. Bind validation rules từ DataAnnotations

---

## 4. `Views/Account/Register.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.RegisterViewModel` (trong AppUser.cs)

**Form fields:** FullName, UserName, Email, Password (type=password), ConfirmPassword

**Validation quan trọng:**
- `[Compare(nameof(Password))]` trên ConfirmPassword → client validation tự so sánh 2 fields
- `[StringLength(100, MinimumLength=6)]` trên Password → validate độ dài

---

## 5. `Views/Shop/Index.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.ShopViewModel`

**Mục đích:** Trang danh sách sản phẩm với sidebar filter và grid sản phẩm.

**Cấu trúc layout Bootstrap:**
```html
<div class="row g-4">
    <aside class="col-lg-3">  <!-- Sidebar filter -->
    <div class="col-lg-9">    <!-- Products grid -->
```

**Sidebar Filter Form** (GET method):
- Radio buttons cho danh mục (từ `Model.Categories`)
- Input range giá `minPrice` / `maxPrice`
- Hidden inputs giữ `keyword`, `sort` hiện tại
- Button "Áp dụng" submit form

**Thanh công cụ trên grid:**
- Đếm: "Hiển thị X / Y sản phẩm"
- Dropdown sắp xếp: `onchange="this.form.submit()"` — auto submit khi chọn

**Product Grid:** `row g-4` với `col-6 col-md-4 col-xl-4` — responsive 2/3/3 columns. Render `_ProductCard` partial.

**Pagination:**
```cshtml
@for (var i = 1; i <= Model.TotalPages; i++)
{
    <li class="page-item @(i == Model.Page ? "active" : "")">
        <a class="page-link" asp-route-page="@i" asp-route-categoryId="@Model.CategoryId" ...>
```
Tag Helpers `asp-route-*` tự build query string.

**Anti-forgery token:** `@Html.AntiForgeryToken()` ở cuối — cần thiết cho AJAX calls từ JS (Cart add).

---

## 6. `Views/Product/Detail.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.ProductDetailViewModel`

**Inject:** `@inject WishlistService WishlistSvc`

**Mục đích:** Trang chi tiết sản phẩm — gallery ảnh, thông tin, tabs (Mô tả / Thông số / Đánh giá), sản phẩm liên quan.

**Image Gallery:**
```cshtml
var images = new List<string>();
if (!string.IsNullOrEmpty(p.ImageUrl)) images.Add(p.ImageUrl);
if (p.ImageUrls is { Count: > 0 }) images.AddRange(p.ImageUrls);
```
Ảnh thumbnail khi click → đổi ảnh chính (JavaScript). Hover trên ảnh chính → zoom 1.6x.

**Badges động:**
```cshtml
@if (p.IsHot)            { <span class="badge-pill badge-hot">HOT</span> }
@if (p.IsNew)            { <span class="badge-pill badge-new">NEW</span> }
@if (p.DiscountPercent > 0) { <span class="badge-pill badge-sale">-@p.DiscountPercent%</span> }
```

**Quantity Stepper + Add to Cart:**
- Input số lượng tăng/giảm bằng nút +/-
- Button "Thêm vào giỏ" với `data-add-to-cart="@p.Id"` và `data-qty-source="#qtyInput"`
- JavaScript đọc qty từ input, gửi AJAX POST tới `/Cart/Add`

**Wishlist toggle:**
```cshtml
var liked = WishlistSvc.Contains(p.Id);
<button class="btn-icon @(liked ? "active" : "")" data-wishlist="@p.Id">
    <i class="bi @(liked ? "bi-heart-fill" : "bi-heart")"></i>
</button>
```
Trạng thái heart icon được set từ server, JS toggle khi click.

**Tab Reviews — form gửi đánh giá:**
```html
<form asp-controller="Product" asp-action="Review" method="post">
    <input type="hidden" name="ProductId" value="@p.Id" />
    <input name="CustomerName" value="@User.Identity?.Name" />
    <div class="rating-input">  <!-- Radio stars 5→1 -->
    <textarea name="Comment" ...></textarea>
</form>
```

**JavaScript (section Scripts):**
- Image zoom + thumb switching
- Hover zoom effect
- Sync qty cho Add to Cart / Buy Now

---

## 7. `Views/Cart/Index.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.ShoppingCart`

**Mục đích:** Trang giỏ hàng đầy đủ.

**Trường hợp giỏ trống:** Hiển thị icon + message + link về Shop.

**Bảng sản phẩm (`#cartTbody`):**
| Cột | Nội dung |
|---|---|
| Sản phẩm | Ảnh + tên (link detail) |
| Đơn giá | `i.Price.ToString("#,##0")đ` |
| Số lượng | Qty stepper (+ / -) |
| Thành tiền | `i.LineTotal.ToString("#,##0")đ` |
| Xoá | Button trash |

**AJAX update số lượng (JavaScript):**
```javascript
async function syncRow(tr, qty) {
    const r = await fetch('/Cart/Update', {
        method: 'POST',
        body: new URLSearchParams({ productId: id, quantity: qty, __RequestVerificationToken: ... })
    });
    const cart = await r.json();
    // Cập nhật UI: số lượng, line total, subtotal, shipping, total
}
```
Khi qty = 0 → gọi `/Cart/Remove` (qty=0 → CartService.Update tự xoá). Khi giỏ rỗng → `window.location.reload()`.

**Order Summary sidebar:**
- Subtotal, ShippingFee (Miễn phí nếu >= 500K)
- Total
- Nút "Tiến hành thanh toán" → `/Checkout`

---

## 8. `Views/Checkout/Index.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.CheckoutViewModel`

**ViewBag:** `ViewBag.Cart` (ShoppingCart)

**Form:** POST tới `CheckoutController.Confirm`

**Cấu trúc:**
- Cột trái (7/12): Thông tin giao hàng + Phương thức thanh toán
- Cột phải (5/12): Order summary (list items + tổng)

**Thông tin giao hàng:**
- `asp-for="CustomerName"`, `Phone`, `Email`, `ShippingAddress`, `Notes`
- `<span asp-validation-for="...">` — hiển thị lỗi từng field

**Phương thức thanh toán** (radio buttons):
```cshtml
@{ var pms = new (string Code, string Label, string Icon)[] {
    ("COD", "COD", "bi-cash-coin"),
    ("VNPay", "VNPay", "bi-qr-code"),
    ("Momo", "Ví Momo", "bi-wallet2"),
    ("BankTransfer", "Chuyển khoản", "bi-bank")
}; }
```
Render 4 lựa chọn thanh toán bằng vòng lặp.

---

## 9. `Views/Home/Index.cshtml`

**Model:** không có @model (dùng ViewBag)

**Sections:**
- **Hero banner:** Tiêu đề, CTA buttons (Mua ngay → Shop, xem HOT deals)
- **Categories showcase:** Grid 4 danh mục với icon + description
- **Featured (HOT):** Carousel/grid sản phẩm HOT (`ViewBag.Featured`)
- **New Arrivals:** Sản phẩm mới nhất (`ViewBag.NewArrivals`)
- **Top Sellers:** Bán chạy nhất (`ViewBag.TopSellers`)

---

## 10. `Areas/Admin/Views/Shared/_AdminLayout.cshtml`

**Mục đích:** Layout riêng cho khu vực Admin. Khác với `_Layout.cshtml` (không có cart drawer, wishlist).

**Cấu trúc:**
- **Sidebar trái:** Menu navigation Admin (Dashboard, Sản phẩm, Danh mục, Đơn hàng, Đánh giá, Người dùng)
- **Main content:** `@RenderBody()`
- **Topbar:** Tên admin + nút đăng xuất

**ViewStart Admin** (`Areas/Admin/Views/_ViewStart.cshtml`):
```cshtml
@{ Layout = "~/Areas/Admin/Views/Shared/_AdminLayout.cshtml"; }
```
Các view trong Admin Area tự động dùng AdminLayout.

---

## 11. `Areas/Admin/Views/Dashboard/Index.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.DashboardViewModel`

**Nội dung:**
- **4 stat cards:** Doanh thu tháng, Tổng đơn, Sản phẩm, Người dùng
- **Line chart** (Chart.js): Doanh thu 14 ngày (trục Y1) + số đơn (trục Y2)
- **Doughnut chart** (Chart.js): Số SP theo danh mục
- **Bảng đơn hàng gần đây**
- **Top 5 sản phẩm bán chạy**

**Chart.js integration:**
```cshtml
const labels = @Html.Raw(JsonSerializer.Serialize(Model.RevenueLabels));
const revSeries = @Html.Raw(JsonSerializer.Serialize(Model.RevenueSeries));
```
`Html.Raw` serialize C# List thành JSON array trực tiếp vào JavaScript.

**Section Scripts:** Load `chart.js` từ CDN + init code. Views/Admin dùng `@section Scripts { ... }` kế thừa từ AdminLayout.

---

## 12. `Areas/Admin/Views/Products/Add.cshtml` và `Update.cshtml`

**Model:** `@model WebBanHang_Bai2.Models.Product`

**Form fields Admin Add/Update:**
- Tên sản phẩm (`asp-for="Name"`)
- Giá / Giá gốc
- Mô tả ngắn / Mô tả chi tiết
- Dropdown danh mục (từ `ViewBag.Categories` — SelectList)
- Tồn kho / IsHot / IsNew
- Upload ảnh chính: `<input type="file" name="mainImage">`
- Upload ảnh phụ: `<input type="file" name="newImages" multiple>`

**Upload file:** Form phải có `enctype="multipart/form-data"` để submit file.

**Preview ảnh:** JavaScript preview ảnh trước khi upload.

---

## Tổng hợp — Kỹ thuật Razor thường gặp

### Tag Helpers (thay cho HTML thuần)

| Tag Helper | Thay cho | Tác dụng |
|---|---|---|
| `asp-controller`, `asp-action` | `href` thủ công | Tự build URL đúng |
| `asp-for="Name"` | `name="Name"` | Bind property, validation, value |
| `asp-validation-for="Name"` | `<span>` thủ công | Hiện lỗi validation |
| `asp-validation-summary` | HTML thủ công | Hiện tất cả lỗi ModelState |
| `asp-route-id="@item.Id"` | Query string thủ công | Thêm route param |
| `asp-append-version="true"` | Manual cache-bust | Thêm hash vào URL static file |

### ViewBag vs Model

- **@model (strongly typed):** Được compile-time check, IntelliSense đầy đủ. Ví dụ: `@model ShopViewModel` → `@Model.Products`
- **ViewBag (dynamic):** Không compile-time check. Dùng cho data phụ không muốn thêm vào ViewModel. Ví dụ: `ViewBag.Featured` trong HomeController.

### Partial Views

```cshtml
<partial name="_ProductCard" model="p" />        // Truyền model cụ thể
@Html.AntiForgeryToken()                         // Render hidden CSRF token
@await RenderSectionAsync("Scripts", required: false)  // Section từ child view
```

### Razor Syntax phổ biến

```cshtml
@{ var liked = WishlistSvc.Contains(p.Id); }    // Code block
@if (condition) { ... }                          // If/else
@foreach (var p in Model.Products) { ... }       // Loop
@(p.Price.ToString("#,##0"))đ                    // Expression
@Html.Raw(jsonString)                            // Render không escape HTML
```
