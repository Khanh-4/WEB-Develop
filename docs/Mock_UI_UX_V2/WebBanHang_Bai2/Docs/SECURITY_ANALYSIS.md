# SECURITY ANALYSIS — TechStore

> Phân tích các cơ chế bảo mật được triển khai trong project.
> Source: `Program.cs`, `Controllers/AccountController.cs`, toàn bộ Controllers, `Views/`.

---

## 1. ASP.NET Core Identity — Xác thực và Phân quyền

### 1.1 Cấu hình Identity (Program.cs:16)

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opts =>
{
    opts.Password.RequireDigit           = false;
    opts.Password.RequiredLength         = 6;
    opts.Password.RequireNonAlphanumeric = false;
    opts.Password.RequireUppercase       = false;
    opts.Password.RequireLowercase       = false;
    opts.SignIn.RequireConfirmedAccount  = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

**Điểm mạnh:**
- Identity framework đã battle-tested, được Microsoft maintain
- Mật khẩu hash bằng **PBKDF2 + SHA256 + 128-bit salt + 10.000 iterations** — cực kỳ khó brute-force
- `SecurityStamp` tự động vô hiệu hóa tất cả cookie cũ khi user đổi password/role

**Điểm cần lưu ý (phù hợp với demo):**
- Password policy đã giảm nhẹ (không cần uppercase, digit, special char) → dễ demo nhưng kém an toàn cho production
- `RequireConfirmedAccount = false` → không cần xác nhận email → phù hợp học tập
- `lockoutOnFailure: false` trong Login → không lock account sau nhiều lần sai → cần bật cho production

---

### 1.2 Cookie Authentication

**Cấu hình (Program.cs:28):**
```csharp
builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.LogoutPath       = "/Account/Logout";
    opt.AccessDeniedPath = "/Account/AccessDenied";
    opt.ExpireTimeSpan   = TimeSpan.FromDays(7);
    opt.SlidingExpiration = true;
    opt.Cookie.Name      = "TechStore.Auth";
});
```

**Cơ chế hoạt động:**
1. Sau đăng nhập thành công, Identity tạo **Claims Principal** (chứa UserName, UserId, Roles...)
2. Claims được **encrypt** (AES-256-CBC + HMAC-SHA256) bằng Data Protection API
3. Encrypted claims lưu trong Cookie `TechStore.Auth`
4. Mỗi request, middleware decrypt cookie và set `HttpContext.User`

**`HttpOnly`:** Cookie mặc định có `HttpOnly = true` — JavaScript không đọc được `document.cookie` với cookie này → ngăn XSS đánh cắp cookie.

**`SlidingExpiration = true`:** Mỗi request gia hạn thêm 7 ngày → user không bị logout khi đang dùng tích cực.

**`Secure` flag:** Chưa set explicitly. Nên thêm `opt.Cookie.SecurePolicy = CookieSecurePolicy.Always` cho production (HTTPS only).

---

### 1.3 Role-Based Authorization (RBAC)

**Hai roles trong system:**
- `"Admin"` — quản trị toàn bộ hệ thống
- `"Customer"` — người dùng thông thường

**Áp dụng:**

```csharp
// Yêu cầu đăng nhập (bất kỳ role nào)
[Authorize]
public async Task<IActionResult> Profile() { ... }

// Yêu cầu role Admin cụ thể
[Authorize(Roles = "Admin")]
public class DashboardController : Controller { ... }
```

**Controllers có `[Authorize(Roles = "Admin")]` (class level):**
- `DashboardController`
- `ProductsController` (Admin Area)
- `CategoriesController` (Admin Area)
- `OrdersController` (Admin Area)
- `ReviewsController` (Admin Area)
- `UsersController` (Admin Area)

**Controllers có `[Authorize]` (class level):**
- `CheckoutController` — yêu cầu đăng nhập, không giới hạn role

**Khi access bị từ chối:**
- Chưa đăng nhập → redirect `/Account/Login?returnUrl=...`
- Đã đăng nhập nhưng sai role → redirect `/Account/AccessDenied` (HTTP 403)

---

## 2. CSRF Protection — `[ValidateAntiForgeryToken]`

### Cơ chế

**CSRF (Cross-Site Request Forgery):** Kẻ tấn công dụ user click link/load ảnh từ site khác → trình duyệt tự gửi cookie → request độc hại được thực thi.

**Giải pháp:** Anti-forgery token — server tạo token ngẫu nhiên, nhúng vào form, verify khi POST.

**Trong project:**

Form Razor tự động nhúng token qua Tag Helpers:
```html
<!-- Kết quả render -->
<form method="post" action="/Account/Login">
    <input name="__RequestVerificationToken" type="hidden" value="CfDJ8..." />
    ...
</form>
```

Server verify qua `[ValidateAntiForgeryToken]`:
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginViewModel vm) { ... }
```

**Tất cả POST actions trong project đều có `[ValidateAntiForgeryToken]`** — được áp dụng nhất quán.

**AJAX calls:** JavaScript thủ công lấy token và gửi trong body:
```javascript
// Cart/Index.cshtml:75
body: new URLSearchParams({
    productId: id,
    quantity: qty,
    __RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
})
```

**`@Html.AntiForgeryToken()`** được thêm vào các Views có AJAX (Shop/Index.cshtml, Product/Detail.cshtml) để JavaScript có thể lấy token dù không có form submit.

---

## 3. Input Validation — DataAnnotations

### Server-side Validation

Tất cả model đều có DataAnnotations validate phía server:

```csharp
// Models/Product.cs
[Required(ErrorMessage = "Tên sản phẩm không được để trống")]
[StringLength(150)]
public string Name { get; set; }

[Range(0.01, 1_000_000_000, ErrorMessage = "Giá phải lớn hơn 0")]
public decimal Price { get; set; }
```

**Controller luôn check `ModelState.IsValid`:**
```csharp
if (!ModelState.IsValid) return View(vm); // Trả lại form với lỗi
```

### Client-side Validation

`<partial name="_ValidationScriptsPartial" />` — load **jQuery Validation + Unobtrusive Validation** → validate ngay trên browser, không cần round-trip server.

**Nguyên tắc:** Validate cả 2 phía — client (UX tốt) và server (bảo mật). **Không bao giờ chỉ validate phía client.**

---

## 4. Open Redirect Prevention

```csharp
// AccountController.cs:51
if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    return Redirect(returnUrl);
```

**`Url.IsLocalUrl()`:** Chỉ redirect về URLs cùng domain (bắt đầu bằng `/` hoặc `//` nhưng cùng host). Ngăn attacker redirect tới `https://evil.com` sau đăng nhập.

---

## 5. File Upload Security

```csharp
// Areas/Admin/Controllers/ProductsController.cs:149
var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName)}";
```

**Biện pháp đã có:**
- **Random GUID filename:** Không dùng tên file gốc từ user → ngăn path traversal, tránh overwrite file hệ thống
- **Lưu vào `wwwroot/images/`:** Không lưu vào thư mục có thể execute (như `/bin/`)
- **Size limit 100MB:** `builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 104_857_600)`

**Điểm cần cải thiện:**
- Chưa validate MIME type thực tế của file (chỉ lấy extension từ filename)
- Chưa validate kích thước tối đa cho từng loại (ảnh nên giới hạn ~5MB)
- Chưa kiểm tra extension có nằm trong whitelist (`jpg`, `png`, `svg`, `webp`)

**Ví dụ cải thiện:**
```csharp
private static readonly HashSet<string> AllowedExtensions = new() { ".jpg", ".jpeg", ".png", ".svg", ".webp" };

private async Task<string> SaveImage(IFormFile image)
{
    var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
    if (!AllowedExtensions.Contains(ext))
        throw new InvalidOperationException("Chỉ chấp nhận ảnh JPG, PNG, SVG, WEBP.");
    if (image.Length > 5 * 1024 * 1024)
        throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");
    // ...
}
```

---

## 6. Session Security

```csharp
// Program.cs:52
builder.Services.AddSession(opt =>
{
    opt.IdleTimeout       = TimeSpan.FromHours(2);
    opt.Cookie.HttpOnly   = true;
    opt.Cookie.IsEssential = true;
    opt.Cookie.Name       = "TechStore.Session";
});
```

**Cơ chế:**
- Browser nhận cookie `TechStore.Session` chứa **session ID** (GUID ngẫu nhiên)
- Session data thực tế (cart, wishlist) lưu trong **server-side DistributedMemoryCache**
- `HttpOnly = true`: JavaScript không đọc được session cookie

**Timeout:** 2 giờ không hoạt động → session hết hạn → giỏ hàng mất.

**Điểm cần lưu ý:**
- `DistributedMemoryCache` lưu trong RAM của tiến trình → **restart server = mất toàn bộ session** (bao gồm giỏ hàng)
- Production nên dùng Redis, SQL Server session store, hoặc lưu giỏ hàng vào DB cho user đã đăng nhập

---

## 7. SQL Injection Prevention

EF Core sử dụng **parameterized queries** theo mặc định:

```csharp
// EFProductRepository.cs
public Product? GetById(int id) => _context.Products.Find(id);
// EF tạo: SELECT * FROM "Products" WHERE "Id" = $1 (parameterized)
```

LINQ queries không bao giờ nối string SQL trực tiếp → **SQL Injection không thể xảy ra** qua EF Core LINQ.

---

## 8. XSS Prevention

Razor Views tự động HTML-encode output:

```cshtml
@p.Name                    <!-- <script>alert(1)</script> → &lt;script&gt;... (safe) -->
@Html.Raw(jsonString)      <!-- KHÔNG encode — chỉ dùng với data đã tin tưởng -->
```

**`@Html.Raw()` trong Dashboard:**
```cshtml
const labels = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.RevenueLabels));
```
Dữ liệu là `List<string>` từ EF Core (ngày tháng), không phải input của user → an toàn.

---

## 9. Admin Account Protection

```csharp
// Areas/Admin/Controllers/UsersController.cs:43
if (user is not null && user.UserName != "admin")
    await _userManager.DeleteAsync(user);
```

Tài khoản `admin` hardcoded không thể bị xoá qua UI — bảo vệ tránh mất quyền truy cập.

---

## Tổng hợp — Điểm mạnh và cần cải thiện

### Điểm mạnh (production-ready)

| Bảo mật | Trạng thái | Ghi chú |
|---|---|---|
| Password hashing PBKDF2 | Tốt | Identity mặc định, không cần config |
| CSRF Protection | Tốt | 100% POST actions có `[ValidateAntiForgeryToken]` |
| SQL Injection | Tốt | EF Core parameterized queries |
| XSS Prevention | Tốt | Razor auto-encode |
| Role-based Authorization | Tốt | Admin/Customer separation |
| Open Redirect Prevention | Tốt | `Url.IsLocalUrl()` |
| Session HttpOnly | Tốt | Cookie không đọc được bởi JS |

### Điểm cần cải thiện (cho production)

| Vấn đề | Mức độ | Giải pháp |
|---|---|---|
| Password policy yếu | Trung bình | Bật RequireDigit, RequireUppercase |
| Không lock account | Cao | `lockoutOnFailure: true` |
| File upload không validate MIME | Trung bình | Validate extension + MIME type |
| Session in-memory | Thấp (dev) | Redis/SQL Server cho production |
| Cookie không force HTTPS | Trung bình | `SecurePolicy = Always` |
| Không có rate limiting | Cao | Chống brute-force login |
| Không có logging bảo mật | Trung bình | Log failed login attempts |
| Không xác nhận email | Thấp | Bật `RequireConfirmedAccount = true` |
