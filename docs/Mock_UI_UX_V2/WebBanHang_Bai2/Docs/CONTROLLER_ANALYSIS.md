# CONTROLLER ANALYSIS — TechStore

> Source: `Controllers/` và `Areas/Admin/Controllers/`

Controllers trong ASP.NET Core MVC đóng vai trò **điều phối trung tâm**: nhận HTTP request, thực hiện business logic (thường qua Repository), chuẩn bị data cho View, rồi trả kết quả về client.

---

## Quy ước chung trong project

- **Dependency Injection:** Tất cả dependencies (repository, service) được inject qua constructor.
- **`[ValidateAntiForgeryToken]`:** Bắt buộc với mọi POST action để chống CSRF.
- **`TempData["Success"]` / `TempData["Error"]`:** Thông báo hiển thị 1 lần sau redirect (render trong `_Layout.cshtml`).
- **Repository Pattern:** Controller không gọi thẳng DbContext, luôn qua interface `IProductRepository`, `IOrderRepository`, v.v.

---

## 1. HomeController (`Controllers/HomeController.cs`)

**Inject:** `IProductRepository`, `ICategoryRepository`

### Actions

| Action | Method | URL | Chức năng |
|---|---|---|---|
| `Index()` | GET | `/` hoặc `/Home/Index` | Trang chủ |
| `About()` | GET | `/Home/About` | Trang giới thiệu |
| `Contact()` | GET | `/Home/Contact` | Trang liên hệ |
| `Privacy()` | GET | `/Home/Privacy` | Chính sách |
| `Error()` | GET | `/Home/Error` | Trang lỗi |

### `Index()` — chi tiết

**Luồng:**
1. Gọi `_products.GetAll()` lấy toàn bộ sản phẩm
2. Lọc `IsHot == true`, lấy tối đa 8 → `ViewBag.Featured`
3. Sắp xếp theo `CreatedAt` giảm dần, lấy 8 → `ViewBag.NewArrivals`
4. Sắp xếp theo `Sold` giảm dần, lấy 8 → `ViewBag.TopSellers`
5. Lấy danh sách danh mục → `ViewBag.Categories`
6. Trả `View()` (không truyền Model, dùng ViewBag)

**Kết quả:** `Views/Home/Index.cshtml`

### `Error()`

Dùng `ResponseCache` (Duration=0, NoStore=true) để tránh browser cache trang lỗi. Truyền `ErrorViewModel` với `RequestId`.

---

## 2. ShopController (`Controllers/ShopController.cs`)

**Inject:** `IProductRepository`, `ICategoryRepository`

### Actions

| Action | Method | URL | Chức năng |
|---|---|---|---|
| `Index(...)` | GET | `/Shop?categoryId=1&keyword=...` | Danh sách + lọc + tìm kiếm + phân trang |

### `Index()` — chi tiết (luồng step-by-step)

**Tham số nhận từ query string:**
- `categoryId` (int?) — lọc theo danh mục
- `keyword` (string?) — từ khóa tìm kiếm
- `sort` (string, default "newest") — kiểu sắp xếp
- `minPrice` / `maxPrice` (decimal?) — lọc giá
- `page` (int, default 1) — trang hiện tại
- `pageSize` (int, default 12) — số SP mỗi trang

**Bước 1:** `_products.GetAll().AsEnumerable()` — lấy tất cả SP

**Bước 2:** Filter:
```csharp
if (categoryId.HasValue)  query = query.Where(p => p.CategoryId == categoryId.Value);
if (keyword != null)      query = query.Where(p => p.Name.Contains(kw) || ...);
if (minPrice.HasValue)    query = query.Where(p => p.Price >= minPrice.Value);
if (maxPrice.HasValue)    query = query.Where(p => p.Price <= maxPrice.Value);
```

**Bước 3:** Sort (switch expression):
```
"price_asc"  → OrderBy(Price)
"price_desc" → OrderByDescending(Price)
"name_asc"   → OrderBy(Name)
"name_desc"  → OrderByDescending(Name)
"rating"     → OrderByDescending(Rating).ThenByDescending(ReviewCount)
"hot"        → OrderByDescending(Sold)
default      → OrderByDescending(CreatedAt)
```

**Bước 4:** Count tổng trước khi paginate → `TotalItems`

**Bước 5:** Paginate: `.Skip((page-1) * pageSize).Take(pageSize).ToList()`

**Bước 6:** Build `ShopViewModel` và trả `View(vm)`

**Lưu ý:** Vì gọi `.AsEnumerable()`, filter/sort xảy ra trong RAM (LINQ to Objects), không phải SQL. Với dataset nhỏ (<1000 SP) OK, nhưng khi scale cần chuyển sang IQueryable và filter trước `.ToList()`.

---

## 3. ProductController (`Controllers/ProductController.cs`)

**Inject:** `IProductRepository`, `IReviewRepository`

### Actions

| Action | Method | URL | Chức năng |
|---|---|---|---|
| `Detail(int id)` | GET | `/Product/Detail/5` | Trang chi tiết sản phẩm |
| `Review(Review)` | POST | `/Product/Review` | Gửi đánh giá sản phẩm |

### `Detail(int id)` — luồng:

1. `_products.GetById(id)` — nếu null → `NotFound()` (404)
2. Lấy SP cùng danh mục, tối đa 4, loại SP hiện tại → `Related`
3. `_reviews.GetByProduct(id)` → danh sách review
4. Build `ProductDetailViewModel` → View

### `Review(Review review)` — POST, `[ValidateAntiForgeryToken]`:

1. Validate `ModelState` — nếu lỗi → TempData["Error"] + Redirect về Detail
2. `_reviews.Add(review)` — lưu review
3. Tính lại `Rating = Average` và `ReviewCount` cho Product
4. `_products.Update(p)` — cập nhật sản phẩm
5. TempData["Success"] + Redirect về Detail

---

## 4. AccountController (`Controllers/AccountController.cs`)

**Inject:** `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `IOrderRepository`

### Actions

| Action | Method | Authorize | Chức năng |
|---|---|---|---|
| `Login(string? returnUrl)` | GET | — | Hiển thị form đăng nhập |
| `Login(LoginViewModel, returnUrl)` | POST | — | Xử lý đăng nhập |
| `Register()` | GET | — | Hiển thị form đăng ký |
| `Register(RegisterViewModel)` | POST | — | Xử lý đăng ký |
| `Logout()` | POST | — | Đăng xuất |
| `AccessDenied()` | GET | — | Trang từ chối truy cập |
| `Profile()` | GET | `[Authorize]` | Trang thông tin cá nhân |
| `Orders()` | GET | `[Authorize]` | Lịch sử đơn hàng |

### `Login POST` — luồng chi tiết:

1. `ModelState.IsValid` → nếu không → trả lại View với lỗi
2. `_signInManager.PasswordSignInAsync(userName, password, rememberMe, lockoutOnFailure: false)` — Identity tự kiểm tra password hash, tạo cookie
3. Nếu thất bại → `ModelState.AddModelError` + View
4. Nếu thành công → TempData["Success"] = "Xin chào..."
5. Nếu có `returnUrl` và local → Redirect về returnUrl
6. Kiểm tra role Admin → Redirect `/Admin/Dashboard` hoặc `/Home/Index`

**Tham số `lockoutOnFailure: false`:** Tắt tính năng lock tài khoản sau nhiều lần sai — đơn giản hóa cho demo.

### `Register POST` — luồng:

1. Validate ModelState
2. Tạo `ApplicationUser` mới
3. `_userManager.CreateAsync(user, vm.Password)` — Identity tự hash password (PBKDF2)
4. Nếu lỗi → hiển thị lỗi Identity (VD: "Username already taken")
5. Nếu OK → `AddToRoleAsync(user, "Customer")` + Redirect Login

### `returnUrl` mechanism:

Khi người dùng cố truy cập `/Checkout/Index` mà chưa đăng nhập, Identity middleware redirect tới `/Account/Login?returnUrl=/Checkout/Index`. Sau khi đăng nhập thành công, controller kiểm tra `Url.IsLocalUrl(returnUrl)` (chống Open Redirect) rồi redirect về.

---

## 5. CartController (`Controllers/CartController.cs`)

**Inject:** `CartService`

### Actions — chủ yếu trả JSON (dùng cho AJAX)

| Action | Method | URL | Trả về | Chức năng |
|---|---|---|---|---|
| `Index()` | GET | `/Cart` | View | Trang giỏ hàng đầy đủ |
| `Json()` | GET | `/Cart/Json` | JSON | Lấy giỏ hàng hiện tại (dùng cho drawer) |
| `Add(productId, quantity)` | POST | `/Cart/Add` | JSON | Thêm sản phẩm |
| `Update(productId, quantity)` | POST | `/Cart/Update` | JSON | Cập nhật số lượng |
| `Remove(productId)` | POST | `/Cart/Remove` | JSON | Xoá sản phẩm |
| `Clear()` | POST | `/Cart/Clear` | JSON | Xoá toàn bộ giỏ |

**`Add` — luồng:**
```csharp
try {
    var cart = _cart.Add(productId, quantity);
    return new JsonResult(cart);  // trả ShoppingCart object dạng JSON
} catch (InvalidOperationException ex) {
    return BadRequest(ex.Message);  // SP không tồn tại
}
```

**Tất cả POST actions đều có `[ValidateAntiForgeryToken]`** — JavaScript gửi kèm token trong request body (`__RequestVerificationToken`).

---

## 6. CheckoutController (`Controllers/CheckoutController.cs`)

**Inject:** `CartService`, `IOrderRepository`, `UserManager<ApplicationUser>`

**`[Authorize]` ở class level** — toàn bộ controller yêu cầu đăng nhập.

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index()` | GET | Hiển thị form checkout với thông tin user sẵn |
| `Confirm(CheckoutViewModel)` | POST | Xử lý đặt hàng, lưu Order vào DB |
| `Success(string code)` | GET | Trang xác nhận đặt hàng thành công |

### `Index()` — luồng:

1. Kiểm tra giỏ hàng trống → redirect Cart với Error
2. Lấy thông tin user hiện tại (`FindByNameAsync`)
3. Pre-fill `CheckoutViewModel` từ thông tin user (Name, Email, Phone, Address)
4. `ViewBag.Cart = cart` — truyền giỏ hàng để hiển thị Order Summary
5. Trả View

### `Confirm POST` — luồng (quan trọng nhất):

1. Kiểm tra giỏ trống → redirect Cart
2. `ModelState.IsValid` → nếu không → trả lại View
3. Tạo `Order` object từ form + giỏ hàng:
   ```csharp
   var order = new Order {
       ...,
       Items = cart.Items.Select(i => new OrderDetail { ... }).ToList()
   };
   ```
4. `_orders.Add(order)` — lưu vào PostgreSQL (EFOrderRepository tự tạo OrderCode)
5. `_cart.Clear()` — xoá giỏ hàng khỏi Session
6. TempData["Success"] + Redirect `Success?code=ORD...`

---

## 7. WishlistController (`Controllers/WishlistController.cs`)

**Inject:** `WishlistService`, `IProductRepository`

### Actions

| Action | Method | Trả về | Chức năng |
|---|---|---|---|
| `Index()` | GET | View | Danh sách yêu thích |
| `Toggle(productId)` | POST | JSON `{added, count}` | Thêm/xoá toggle |
| `Remove(productId)` | POST | Redirect | Xoá khỏi wishlist |

**`Toggle`** — JSON response:
```json
{ "added": true, "count": 3 }
```
JavaScript dùng response này để cập nhật UI (đổi màu heart icon, cập nhật badge count).

---

## 8. Admin/DashboardController (`Areas/Admin/Controllers/DashboardController.cs`)

**`[Area("Admin")]`**, **`[Authorize(Roles = "Admin")]`**

**Inject:** `IProductRepository`, `ICategoryRepository`, `IOrderRepository`, `UserManager<ApplicationUser>`

### `Index()` — luồng:

1. Lấy tất cả orders
2. Tính `RevenueMonth` (tháng này, bỏ Cancelled), `RevenueAll`, `OrdersToday`
3. Build mảng 14 ngày qua: label, doanh thu, số đơn → dữ liệu Chart.js
4. Tính số SP theo danh mục → biểu đồ donut
5. Top 5 SP bán chạy (`OrderByDescending(Sold).Take(5)`)
6. 8 đơn gần nhất
7. Build `DashboardViewModel` và trả View

---

## 9. Admin/ProductsController (`Areas/Admin/Controllers/ProductsController.cs`)

**`[Area("Admin")]`**, **`[Authorize(Roles = "Admin")]`**

**Inject:** `IProductRepository`, `ICategoryRepository`, `IWebHostEnvironment`

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index(keyword, categoryId, sort, page, pageSize)` | GET | Danh sách SP với filter + phân trang |
| `Add()` | GET | Form thêm SP mới |
| `Add(Product, IFormFile?, List<IFormFile>?)` | POST | Lưu SP mới + upload ảnh |
| `Update(int id)` | GET | Form chỉnh sửa SP |
| `Update(Product, IFormFile?, List<IFormFile>?)` | POST | Lưu SP đã sửa + upload ảnh mới |
| `Delete(int id)` | POST | Xoá SP |

### Upload ảnh — `SaveImage(IFormFile image)`:

```csharp
// ProductsController.cs:149
var dir = Path.Combine(_env.WebRootPath, "images");
Directory.CreateDirectory(dir);
var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName)}";
var path = Path.Combine(dir, fileName);
await using var fs = new FileStream(path, FileMode.Create);
await image.CopyToAsync(fs);
return $"/images/{fileName}";
```

**Giải thích:** File được lưu vào `wwwroot/images/` với tên là GUID ngẫu nhiên (tránh trùng tên, tránh path traversal). Đường dẫn trả về là `/images/{guid}.{ext}`.

### `[RequestSizeLimit(104_857_600)]` — giới hạn upload 100MB.

### `Add POST` — luồng:

1. Validate ModelState
2. Upload ảnh chính nếu có → `SaveImage` → `product.ImageUrl`
3. Upload ảnh phụ nếu có → `SaveManyAsync` → `product.ImageUrls`
4. Set `product.Category = _categories.GetById(product.CategoryId)?.Name`
5. `_products.Add(product)` → lưu DB
6. TempData["Success"] + Redirect Index

### `Update POST` — luồng:

1. Validate ModelState
2. Load existing product (`GetById`) — nếu null → NotFound
3. Upload ảnh mới nếu có; nếu không có ảnh mới → giữ ảnh cũ
4. Append ảnh phụ mới vào danh sách cũ
5. Giữ nguyên `Rating`, `ReviewCount`, `Sold`, `CreatedAt` từ existing
6. `_products.Update(product)` → lưu DB

### `PopulateCategories(int? selected)`:

Helper method tạo `SelectList` cho dropdown danh mục trong form Add/Update:
```csharp
ViewBag.Categories = new SelectList(_categories.GetAllCategories(), "Id", "Name", selected);
```

---

## 10. Admin/OrdersController (`Areas/Admin/Controllers/OrdersController.cs`)

**`[Area("Admin")]`**, **`[Authorize(Roles = "Admin")]`**

**Inject:** `IOrderRepository`

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index(keyword, status)` | GET | Danh sách đơn hàng với filter |
| `Detail(int id)` | GET | Chi tiết 1 đơn hàng |
| `UpdateStatus(int id, OrderStatus status)` | POST | Cập nhật trạng thái đơn |
| `Delete(int id)` | POST | Xoá đơn hàng |

### `Index` — filter:

- Lọc theo `status` (Pending/Confirmed/Shipping/Completed/Cancelled)
- Tìm theo OrderCode, CustomerName, Phone

### `UpdateStatus POST`:

```csharp
_orders.UpdateStatus(id, status);
TempData["Success"] = $"Đã cập nhật trạng thái đơn #{id} thành {status}.";
return RedirectToAction(nameof(Detail), new { id });
```

---

## 11. Admin/CategoriesController (`Areas/Admin/Controllers/CategoriesController.cs`)

**`[Area("Admin")]`**, **`[Authorize(Roles = "Admin")]`**

**Inject:** `ICategoryRepository`, `IProductRepository`

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index()` | GET | Danh sách danh mục + số SP mỗi loại |
| `Add()` | GET | Form thêm danh mục |
| `Add(Category)` | POST | Lưu danh mục mới |
| `Update(int id)` | GET | Form sửa danh mục |
| `Update(Category)` | POST | Lưu thay đổi |
| `Delete(int id)` | POST | Xoá danh mục |

### `Index` — tính số sản phẩm:

```csharp
ViewBag.ProductCounts = _categories.GetAllCategories()
    .ToDictionary(c => c.Id, c => prods.Count(p => p.CategoryId == c.Id));
```

---

## 12. Admin/ReviewsController (`Areas/Admin/Controllers/ReviewsController.cs`)

**Inject:** `IReviewRepository`, `IProductRepository`

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index()` | GET | Danh sách tất cả đánh giá + tên sản phẩm |
| `Delete(int id)` | POST | Xoá đánh giá |

---

## 13. Admin/UsersController (`Areas/Admin/Controllers/UsersController.cs`)

**Inject:** `UserManager<ApplicationUser>`

### Actions

| Action | Method | Chức năng |
|---|---|---|
| `Index(keyword)` | GET | Danh sách users + role của từng user |
| `Delete(string id)` | POST | Xoá user (bảo vệ account "admin") |

### `Delete` — bảo vệ admin:

```csharp
if (user is not null && user.UserName != "admin")
    await _userManager.DeleteAsync(user);
```

Tài khoản `admin` không thể bị xoá — hardcoded check.

---

## Tổng hợp — Attributes quan trọng

| Attribute | Áp dụng cho | Ý nghĩa |
|---|---|---|
| `[Authorize]` | `CheckoutController` (class level) | Yêu cầu đăng nhập |
| `[Authorize(Roles = "Admin")]` | Tất cả Admin controllers | Chỉ role Admin mới được truy cập |
| `[ValidateAntiForgeryToken]` | Tất cả POST actions | Chống tấn công CSRF |
| `[HttpGet]` / `[HttpPost]` | Các actions | Chỉ nhận GET hoặc POST |
| `[Area("Admin")]` | Admin controllers | Khai báo thuộc Area Admin |
| `[RequestSizeLimit(104857600)]` | Admin Products Add/Update | Giới hạn upload 100MB |
| `[ResponseCache]` | `HomeController.Error` | Không cache trang lỗi |
