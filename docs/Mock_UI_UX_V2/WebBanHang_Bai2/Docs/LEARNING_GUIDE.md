# LEARNING GUIDE — Lộ trình học Project TechStore trong 5 ngày

> Hướng dẫn dành cho sinh viên CNPM năm 2-4 muốn hiểu sâu project để báo cáo và vấn đáp.

---

## Tổng quan lộ trình

```
Ngày 1: Kiến trúc tổng thể + Setup môi trường
Ngày 2: Database + Models + EF Core
Ngày 3: Controllers + Repositories + DI
Ngày 4: Views + Session + Authentication
Ngày 5: Admin Area + Security + Ôn tập vấn đáp
```

---

## NGÀY 1: Kiến trúc tổng thể + Setup

### Mục tiêu

Hiểu được project làm gì, chạy được trên máy, hiểu cấu trúc folder.

### Bước 1: Đọc tổng quan (30 phút)

Đọc file `PROJECT_OVERVIEW.md` (file này). Tập trung:
- Section 2: Stack công nghệ
- Section 3: Kiến trúc MVC — luồng Request → Response
- Section 4: Cấu trúc thư mục

### Bước 2: Chạy project (30 phút)

```bash
cd "Bài 2/WebBanHang_Bai2"
dotnet restore
dotnet run
```

Truy cập `http://localhost:5003`. Thử:
- Xem trang chủ
- Vào Shop, tìm kiếm "laptop"
- Thêm vào giỏ hàng
- Đăng nhập `admin/admin123`
- Vào Admin Dashboard

### Bước 3: Đọc file cấu hình (45 phút)

Đọc theo thứ tự:
1. `WebBanHang_Bai2.csproj` — hiểu dependencies
2. `appsettings.json` — connection string, logging
3. `Program.cs` — toàn bộ, từng dòng

**Câu hỏi tự kiểm tra:**
- Tại sao Repositories dùng `Scoped` chứ không phải `Singleton`?
- Middleware nào đứng trước/sau Auth?
- `DbSeeder.SeedAsync` được gọi ở đâu?

### Bước 4: Đọc cấu trúc folder (30 phút)

Mở VS Code / Visual Studio, nhìn toàn bộ cây folder. Đặt câu hỏi: file này thuộc layer nào? (Model/View/Controller/Service/Repository?)

---

## NGÀY 2: Database + Models + EF Core

### Mục tiêu

Hiểu schema database, từng Model, cách EF Core hoạt động.

### Bước 1: Đọc DATABASE_ANALYSIS.md (45 phút)

Đặc biệt chú ý:
- Tại sao `ImageUrls` lưu JSON?
- Tại sao `DiscountPercent` không lưu DB?
- Sự khác biệt FK thật vs FK logic (soft reference)
- `OwnsMany` — OrderDetail là "owned entity"

### Bước 2: Đọc Models (60 phút)

Đọc từng file trong thứ tự:
1. `Models/Product.cs` — entity chính
2. `Models/Category.cs` — entity đơn giản
3. `Models/ApplicationUser.cs` — kế thừa IdentityUser, `[NotMapped]`
4. `Models/Order.cs` — enum, OrderDetail, CheckoutViewModel
5. `Models/Review.cs`
6. `Models/Cart.cs` — ShoppingCart methods (Add/Update/Remove/Clear)
7. `Models/ShopViewModel.cs` — tất cả ViewModels
8. `Models/AppUser.cs` — legacy + RegisterViewModel
9. `Models/LoginViewModel.cs`

### Bước 3: Đọc EF Core setup (30 phút)

1. `Data/ApplicationDbContext.cs` — `OnModelCreating`, `HasConversion`, `OwnsMany`
2. `Data/DbSeeder.cs` — dữ liệu seed thực tế

### Bước 4: Đọc Migration (30 phút)

`Migrations/20260605073155_Initial.cs`:
- Bảng nào được tạo?
- Kiểu dữ liệu PostgreSQL cho từng column?
- FK constraints ở đâu?

### Bước 5: Đọc MODEL_ANALYSIS.md

Dùng như reference và để tự kiểm tra hiểu đúng không.

**Bài tập thực hành:**
```bash
# Kết nối database (nếu có creds) và check các bảng
# Hoặc thêm 1 sản phẩm mới qua Admin UI → kiểm tra xem trong DB thế nào
```

---

## NGÀY 3: Controllers + Repositories + DI

### Mục tiêu

Hiểu Repository Pattern, DI, từng Controller và Action.

### Bước 1: Đọc Repositories (45 phút)

Theo thứ tự:
1. `Repositories/IProductRepository.cs` — interface là gì?
2. `Repositories/EFProductRepository.cs` — EF Core implementation
3. `Repositories/MockProductRepository.cs` — List in-memory
4. `Repositories/ICategoryRepository.cs` + `EFCategoryRepository.cs`
5. `Repositories/IOrderRepository.cs` — interface + MockOrderRepository
6. `Repositories/EFOrderRepository.cs` — `Include(o => o.Items)` tại sao cần?
7. `Repositories/IReviewRepository.cs` + `EFReviewRepository.cs`

**So sánh EF vs Mock:**
| | EFProductRepository | MockProductRepository |
|---|---|---|
| Lưu ở đâu | PostgreSQL | List<> trong RAM |
| Restart server | Giữ data | Mất data |
| Dùng khi nào | Production | Testing/dev đơn giản |

### Bước 2: Đọc Services (30 phút)

1. `Services/SessionExtensions.cs` — SetObject/GetObject là gì?
2. `Services/CartService.cs` — từng method: GetCart, Add, Update, Remove, Clear
3. `Services/WishlistService.cs` — Toggle logic với HashSet

### Bước 3: Đọc Controllers customer-facing (60 phút)

Đọc theo thứ tự nghiệp vụ:
1. `Controllers/HomeController.cs` — ViewBag.Featured, NewArrivals, TopSellers
2. `Controllers/ShopController.cs` — filter pipeline, sort switch, pagination
3. `Controllers/ProductController.cs` — Detail + Review (update Rating)
4. `Controllers/AccountController.cs` — Login, Register, Logout, Profile, Orders
5. `Controllers/CartController.cs` — JsonResult, ValidateAntiForgeryToken
6. `Controllers/CheckoutController.cs` — Authorize, Confirm flow
7. `Controllers/WishlistController.cs` — Toggle JSON

### Bước 4: Đọc CONTROLLER_ANALYSIS.md

Dùng làm reference cho từng action.

**Bài tập:**
- Đặt breakpoint (hoặc thêm Console.WriteLine) trong ShopController.Index
- Chạy app, vào `/Shop?keyword=laptop`
- Quan sát luồng chạy

---

## NGÀY 4: Views + Session + Authentication

### Mục tiêu

Hiểu Razor syntax, Tag Helpers, Bootstrap, AJAX, Session, Identity cookie.

### Bước 1: Đọc Views Shared (30 phút)

1. `Views/Shared/_Layout.cshtml` — inject service, authentication-aware menu, cart drawer
2. `Views/Shared/_ProductCard.cshtml` — partial view tái sử dụng
3. `Views/_ViewStart.cshtml` và `Views/_ViewImports.cshtml`

### Bước 2: Đọc Views chính (60 phút)

Theo luồng user:
1. `Views/Home/Index.cshtml` — ViewBag, sections
2. `Views/Shop/Index.cshtml` — ShopViewModel, sidebar filter, pagination
3. `Views/Product/Detail.cshtml` — Gallery JS, tab-pane, form review
4. `Views/Cart/Index.cshtml` — AJAX JavaScript, syncRow function
5. `Views/Checkout/Index.cshtml` — CheckoutViewModel, payment methods
6. `Views/Account/Login.cshtml` — asp-for, validation summary
7. `Views/Account/Register.cshtml` — Compare attribute

### Bước 3: Đọc VIEW_ANALYSIS.md (30 phút)

Đặc biệt chú ý bảng Tag Helpers và sự khác biệt ViewBag vs @model.

### Bước 4: Đọc và thực hành (30 phút)

Mở DevTools (F12) trong browser:
- Tab Application → Cookies: xem `TechStore.Auth`, `TechStore.Session`
- Tab Network: click "Thêm vào giỏ" → xem AJAX request tới `/Cart/Add`
- Response JSON là gì?

**Bài tập:**
Sửa 1 dòng trong `_Layout.cshtml` (ví dụ đổi text "TechStore" thành tên mình), save, refresh browser — xem runtime compilation hoạt động không.

---

## NGÀY 5: Admin Area + Security + Ôn tập vấn đáp

### Bước 1: Đọc Admin Controllers (45 phút)

1. `Areas/Admin/Controllers/DashboardController.cs` — thống kê, Chart.js data
2. `Areas/Admin/Controllers/ProductsController.cs` — CRUD + file upload
3. `Areas/Admin/Controllers/OrdersController.cs` — UpdateStatus
4. `Areas/Admin/Controllers/CategoriesController.cs`
5. `Areas/Admin/Controllers/ReviewsController.cs`
6. `Areas/Admin/Controllers/UsersController.cs` — admin protection

### Bước 2: Đọc Admin Views (30 phút)

1. `Areas/Admin/Views/Shared/_AdminLayout.cshtml`
2. `Areas/Admin/Views/Dashboard/Index.cshtml` — Chart.js integration
3. `Areas/Admin/Views/Products/Add.cshtml` — file upload form

### Bước 3: Đọc SECURITY_ANALYSIS.md (30 phút)

Hiểu và nhớ các điểm:
- PBKDF2 password hashing
- Cookie authentication flow
- CSRF protection (ValidateAntiForgeryToken)
- Open Redirect prevention
- File upload security

### Bước 4: Đọc BUSINESS_FLOW.md (30 phút)

Nhìn vào từng flowchart, tự giải thích không cần xem code.

### Bước 5: Ôn tập INTERVIEW_PREPARATION.md (90 phút)

Chiến lược ôn tập:
1. Đọc hết 60 câu hỏi + đáp án 1 lần
2. Tự test: che đáp án, đọc câu hỏi, tự trả lời
3. Ghi ra những câu không trả lời được → đọc lại code liên quan
4. Nhờ bạn hỏi ngẫu nhiên

**Ưu tiên ôn 15 câu quan trọng nhất cho vấn đáp:**
- Câu 1 (MVC), 4 (Repository), 5 (Session), 7 (CSRF), 8 (Authorize), 9 (DI), 10 (Scoped/Singleton)
- Câu 21 (EF Core), 22 (Scoped vs Singleton), 25 (AsEnumerable vs IQueryable)
- Câu 41 (Scale), 42 (Unique index), 46 (SOLID), 47 (Rating update), 57 (Include)

---

## Kiến thức cần nắm vững

### 1. MVC Pattern

```
Request → Routing → Controller Action → Repository → DB
                                     ↓
View (.cshtml) ← ViewModel/Model ←──┘
     ↓
HTML Response → Browser
```

**Nhớ:** Controller không bao giờ render HTML trực tiếp. View không chứa logic. Model đại diện data.

### 2. Entity Framework Core

- `DbContext` = cửa sổ nhìn vào database
- `DbSet<T>` = bảng trong C#
- `LINQ → SQL` qua provider (Npgsql cho PostgreSQL)
- `SaveChanges()` = thực thi INSERT/UPDATE/DELETE thực sự
- `Include()` = tải related data (avoid N+1)
- `AsEnumerable()` vs `AsQueryable()` — filter server-side vs client-side

### 3. LINQ phổ biến trong project

```csharp
_products.GetAll()
    .Where(p => p.CategoryId == id)      // filter
    .OrderByDescending(p => p.CreatedAt) // sort
    .Skip((page-1) * pageSize)           // paginate start
    .Take(pageSize)                      // paginate limit
    .ToList();                           // execute, return List<Product>
```

### 4. Razor Views

```cshtml
@model WebBanHang_Bai2.Models.ShopViewModel   // strongly typed model
@{ var x = 1; }                               // code block
@foreach (var p in Model.Products) { ... }    // loop
@if (condition) { ... } else { ... }          // conditional
@p.Name                                       // expression (auto HTML-encode)
@Html.Raw(json)                               // không encode — cẩn thận XSS
<input asp-for="UserName" />                  // Tag Helper
```

### 5. Dependency Injection

```csharp
// Đăng ký (Program.cs)
builder.Services.AddScoped<IProductRepository, EFProductRepository>();

// Sử dụng (Constructor injection)
public ShopController(IProductRepository products) {
    _products = products;  // DI inject EFProductRepository
}
```

### 6. Authentication & Authorization

```
Đăng nhập → Identity hash check → Tạo ClaimsPrincipal → Mã hóa vào Cookie
Request → Middleware đọc Cookie → Decrypt → Set HttpContext.User
[Authorize] → Check User.Identity.IsAuthenticated
[Authorize(Roles="Admin")] → Check User.IsInRole("Admin")
```

### 7. Session

```
Request → Middleware đọc Cookie "TechStore.Session" → Load từ MemoryCache
CartService.GetCart() → Session.GetString(key) → JSON.Deserialize<ShoppingCart>
CartService.Save(cart) → JSON.Serialize(cart) → Session.SetString(key)
```

---

## Cách chuẩn bị vấn đáp

### Trước buổi vấn đáp

1. **Chạy lại app** một lần, thực hiện đầy đủ các flow: đăng ký → đăng nhập → mua hàng → checkout → xem admin
2. **Đọc lại code** các file quan trọng: `Program.cs`, `AccountController.cs`, `CheckoutController.cs`, `EFProductRepository.cs`, `CartService.cs`
3. **Chuẩn bị sơ đồ** trên giấy (vẽ tay): MVC flow, Database ERD đơn giản, Session flow

### Trong buổi vấn đáp

**Cách trả lời tốt:**
- Bắt đầu bằng định nghĩa ngắn gọn → ví dụ cụ thể từ project
- Trích dẫn tên file và số dòng nếu có thể ("Trong `AccountController.cs:39`...")
- Nếu không chắc → thành thật nói "Em chưa chắc lắm, nhưng theo em hiểu thì..."

**Cấu trúc trả lời mẫu:**
> "Dependency Injection là [định nghĩa]. Trong project này, ví dụ `ShopController` nhận `IProductRepository` qua constructor. DI container tự inject `EFProductRepository` vì đã đăng ký `AddScoped<IProductRepository, EFProductRepository>()` trong `Program.cs`. Lợi ích là Controller không biết cụ thể đang dùng EF hay Mock — dễ thay thế và test."

### Các câu hỏi thường gặp về project cụ thể này

Giảng viên hay hỏi những điểm **đặc biệt** hoặc **quyết định thiết kế**:
- "Tại sao không lưu giỏ hàng vào database?"
- "Tại sao dùng Scoped thay vì Singleton?"
- "EFProductRepository khác MockProductRepository ở điểm nào?"
- "Tại sao OrderDetail lưu snapshot tên/giá thay vì FK?"
- "ValidateAntiForgeryToken bảo vệ khỏi tấn công gì?"
- "Làm sao biết user đang đăng nhập có role Admin trong View?"
- "Nếu restart server, giỏ hàng có mất không? Tại sao?"

---

## Tài nguyên bổ sung

### Tài liệu chính thức (đọc khi cần tra cứu)

- [ASP.NET Core MVC Overview](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview)
- [EF Core Getting Started](https://learn.microsoft.com/en-us/ef/core/get-started)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Session and State Management](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state)
- [Tag Helpers](https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/intro)

### Khi bị mắc kẹt

1. Đọc lại code từ đầu route/request
2. Thêm `Console.WriteLine()` để trace luồng
3. Tìm tên class/method trong project (`Ctrl+Shift+F` để tìm toàn bộ project)
4. Đọc lại phần liên quan trong các file `.md` documentation

---

## Checklist trước vấn đáp

- [ ] Chạy được app trên máy không lỗi
- [ ] Đăng nhập được cả admin và customer
- [ ] Thêm được sản phẩm qua Admin
- [ ] Đặt hàng thành công (end-to-end)
- [ ] Hiểu MVC flow (có thể giải thích không cần xem code)
- [ ] Hiểu DI — tại sao Scoped không phải Singleton
- [ ] Hiểu Session — giỏ hàng lưu ở đâu, serialize/deserialize
- [ ] Hiểu Identity — cookie auth, password hash
- [ ] Hiểu CSRF — tại sao cần ValidateAntiForgeryToken
- [ ] Hiểu Repository Pattern — interface vs implementation
- [ ] Có thể giải thích từng bảng trong database
- [ ] Ôn tập 60 câu hỏi vấn đáp ít nhất 1 lần
