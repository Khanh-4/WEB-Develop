# INTERVIEW PREPARATION — 60 Câu hỏi Vấn đáp TechStore

> Tất cả câu hỏi bám sát **code thực tế** của project. Có trích dẫn file cụ thể.

---

## PHẦN 1: Câu hỏi Cơ bản (20 câu)

---

**Câu 1:** MVC là gì? Trong project này, đâu là Model, đâu là View, đâu là Controller?

**Đáp án:** MVC là pattern phân tách ứng dụng thành 3 phần:
- **Model:** Các class trong `Models/` (Product, Order, Category...) đại diện cho dữ liệu
- **View:** Các file `.cshtml` trong `Views/` — render HTML
- **Controller:** Các class trong `Controllers/` — nhận request, xử lý logic, trả View

---

**Câu 2:** File `Program.cs` làm gì trong project này?

**Đáp án:** `Program.cs` là entry point (điểm khởi động) của ứng dụng. Nó:
1. Đăng ký các services vào DI container (DbContext, Identity, Repositories, Session, CartService...)
2. Cấu hình Identity (password policy, cookie...)
3. Xây dựng middleware pipeline (Static Files → Session → Auth → MVC)
4. Cấu hình routing (areas + default route)
5. Gọi `DbSeeder.SeedAsync` để seed dữ liệu ban đầu

---

**Câu 3:** `DbSeeder.SeedAsync` làm gì và được gọi khi nào?

**Đáp án:** `DbSeeder.SeedAsync` (gọi từ `Program.cs:89`) chạy khi app khởi động:
1. `context.Database.MigrateAsync()` — chạy migration tạo schema DB nếu chưa có
2. Tạo 2 roles: "Admin" và "Customer"
3. Tạo 3 user: admin/admin123, khachhang/123456, minhtuan/123456
4. Tạo 4 danh mục: Laptop, Desktop, Phụ kiện, Màn hình
5. Tạo 12 sản phẩm mẫu
Nếu dữ liệu đã tồn tại → **không seed lại** (check `AnyAsync()` trước).

---

**Câu 4:** Repository Pattern là gì? Tại sao project dùng pattern này?

**Đáp án:** Repository Pattern tạo một lớp trung gian giữa Controller và Database. Thay vì Controller gọi thẳng `_context.Products`, Controller gọi `_products.GetAll()`.

**Lợi ích:**
- Dễ thay thế implementation (Mock → EF) mà không sửa Controller
- Dễ Unit Test (mock interface)
- Tách business logic khỏi data access

Ví dụ: `IProductRepository` có 2 implementation — `EFProductRepository` (dùng PostgreSQL) và `MockProductRepository` (dùng List trong RAM).

---

**Câu 5:** Giỏ hàng trong project được lưu ở đâu? Tại sao không lưu vào Database?

**Đáp án:** Giỏ hàng (`ShoppingCart`) lưu trong **Session**. Session data thực chất lưu trong `DistributedMemoryCache` (RAM server), browser chỉ giữ session ID trong cookie `TechStore.Session`.

**Lý do không lưu DB:**
- Giỏ hàng thay đổi rất thường xuyên (thêm, xoá, cập nhật) → nhiều write operations
- Không cần persist qua restart (acceptable UX trade-off)
- Đơn giản hơn, nhanh hơn cho demo/học tập

**Đánh đổi:** Restart server → mất giỏ hàng của tất cả user.

---

**Câu 6:** `CartService` và `CartController` khác nhau như thế nào?

**Đáp án:**
- **`CartService`** (`Services/CartService.cs`): Business logic — đọc/ghi giỏ hàng vào Session, validate sản phẩm tồn tại, tính toán. Đây là Service Layer.
- **`CartController`** (`Controllers/CartController.cs`): HTTP layer — nhận request, gọi CartService, trả JSON/View về client.

Controller ủy thác hoàn toàn cho Service, không chứa logic nghiệp vụ.

---

**Câu 7:** Giải thích `[ValidateAntiForgeryToken]`. Tại sao cần attribute này?

**Đáp án:** `[ValidateAntiForgeryToken]` bảo vệ khỏi **CSRF (Cross-Site Request Forgery)**.

CSRF: Kẻ tấn công nhúng form hoặc ảnh vào website khác, dụ user (đã đăng nhập TechStore) click → browser tự gửi cookie → request thực thi mà user không biết.

Giải pháp: Server tạo token ngẫu nhiên, nhúng vào form (ẩn), verify khi POST. Vì kẻ tấn công không biết token → request bị reject.

Trong Razor form, token tự động nhúng; AJAX phải lấy thủ công từ DOM (như trong `Cart/Index.cshtml:76`).

---

**Câu 8:** `[Authorize]` và `[Authorize(Roles = "Admin")]` khác nhau như thế nào?

**Đáp án:**
- `[Authorize]`: Chỉ cần đăng nhập, bất kỳ role nào cũng được. Dùng cho `CheckoutController` và `AccountController.Profile`.
- `[Authorize(Roles = "Admin")]`: Phải đăng nhập **và** có role "Admin". Dùng cho tất cả controllers trong `Areas/Admin/`.

Nếu chưa đăng nhập → redirect `/Account/Login`.
Nếu đã đăng nhập nhưng không có role Admin → redirect `/Account/AccessDenied` (403).

---

**Câu 9:** Dependency Injection là gì? Cho ví dụ trong project.

**Đáp án:** DI là pattern inject dependencies vào object từ bên ngoài (qua constructor) thay vì object tự tạo. ASP.NET Core có built-in DI container.

**Đăng ký trong `Program.cs`:**
```csharp
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
builder.Services.AddScoped<CartService>();
```

**Sử dụng trong Controller:**
```csharp
// ShopController.cs:9
public ShopController(IProductRepository products, ICategoryRepository categories)
{
    _products = products;      // DI tự inject EFProductRepository
    _categories = categories;  // DI tự inject EFCategoryRepository
}
```
DI container tự tạo và inject object khi request đến — developer không cần `new EFProductRepository(new DbContext(...))`.

---

**Câu 10:** `Scoped`, `Singleton`, `Transient` là gì? Trong project dùng loại nào?

**Đáp án:**
- **Singleton:** Tạo một instance duy nhất, dùng chung trong suốt vòng đời app
- **Scoped:** Tạo mới mỗi HTTP request, dùng chung trong request đó
- **Transient:** Tạo mới mỗi lần inject

**Trong project (`Program.cs:46`):**
```csharp
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
```
Repositories dùng **Scoped** vì EF Core DbContext cũng Scoped — phải cùng lifetime để không bị lỗi "captive dependency".

`CartService` và `WishlistService` cũng Scoped vì cần `IHttpContextAccessor` (per-request).

---

**Câu 11:** ApplicationUser kế thừa từ đâu? Tại sao lại kế thừa?

**Đáp án:** `ApplicationUser : IdentityUser` (`Models/ApplicationUser.cs`).

`IdentityUser` là class của ASP.NET Core Identity, cung cấp sẵn: `Id`, `UserName`, `Email`, `PasswordHash`, `SecurityStamp`, `LockoutEnabled`, v.v.

Bằng cách kế thừa, project **tái sử dụng** toàn bộ cơ sở hạ tầng Identity (hash password, session management, role management) và chỉ thêm các field nghiệp vụ tùy chỉnh: `FullName`, `Phone`, `Address`, `AvatarUrl`, `CreatedAt`.

---

**Câu 12:** `OwnsMany` trong ApplicationDbContext là gì? OrderDetail được lưu như thế nào?

**Đáp án:** `OwnsMany` là EF Core "Owned Entity" — OrderDetail hoàn toàn phụ thuộc vào Order:
```csharp
// ApplicationDbContext.cs:40
builder.Entity<Order>().OwnsMany(o => o.Items, od => {
    od.WithOwner().HasForeignKey("OrderId");
    od.ToTable("OrderDetails");
});
```
OrderDetail không có DbSet riêng, không thể tồn tại độc lập — khi xoá Order, OrderDetail bị xoá theo (CASCADE).

---

**Câu 13:** Tại sao `Product.ImageUrls` lưu dưới dạng JSON string trong database?

**Đáp án:** Thay vì tạo bảng `ProductImages` riêng (cần JOIN), EF Core serialize `List<string>` → JSON text và lưu vào 1 cột `text`.

Cấu hình trong `ApplicationDbContext.cs:28`:
```csharp
builder.Entity<Product>().Property(p => p.ImageUrls)
    .HasConversion(
        v => JsonSerializer.Serialize(v, null),
        v => JsonSerializer.Deserialize<List<string>>(v, null) ?? new List<string>()
    )
    .HasColumnType("text");
```

**Đánh đổi:** Không thể query/filter bên trong mảng ImageUrls bằng SQL — chỉ phù hợp khi không cần tìm kiếm theo ảnh.

---

**Câu 14:** Giải thích `DiscountPercent` trong `Product.cs`. Tại sao không lưu vào DB?

**Đáp án:**
```csharp
// Models/Product.cs:66
public int DiscountPercent =>
    OldPrice.HasValue && OldPrice.Value > Price
        ? (int)Math.Round((OldPrice.Value - Price) / OldPrice.Value * 100)
        : 0;
```

Là **computed property** — tính tự động từ `OldPrice` và `Price`. Không lưu DB vì:
1. Dữ liệu redundant — có thể tính lại bất cứ lúc nào
2. Nếu lưu DB, khi sửa Price phải nhớ cập nhật DiscountPercent → có thể bất đồng bộ

EF Core bỏ qua qua `.Ignore(p => p.DiscountPercent)`.

---

**Câu 15:** `TempData` là gì? Khác gì với `ViewBag`?

**Đáp án:**
- **`TempData`:** Lưu data cho **1 request tiếp theo** — survive qua redirect. Dùng cho thông báo (Success, Error) sau `RedirectToAction`.
- **`ViewBag`:** Lưu data cho **request hiện tại** — không survive redirect. Dùng để truyền data phụ từ Controller sang View.

Ví dụ: Sau `_products.Add(product)`, controller `RedirectToAction("Index")` — nếu dùng ViewBag thì mất. TempData giữ được sang request tiếp.

---

**Câu 16:** Giải thích cấu trúc URL `/Admin/Products/Update/5`. Route nào xử lý?

**Đáp án:** URL này khớp với route `areas` trong `Program.cs:80`:
```csharp
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
```
- `area = "Admin"` → tìm trong `Areas/Admin/Controllers/`
- `controller = "Products"` → `ProductsController`
- `action = "Update"` → `Update(int id)` action
- `id = 5` → tham số

---

**Câu 17:** `asp-for` Tag Helper hoạt động như thế nào?

**Đáp án:** `asp-for="Name"` trên input tự động:
1. Set `name="Name"` (khớp property name để model binding)
2. Set `id="Name"` (dùng cho label)
3. Set `value="@Model.Name"` (pre-fill từ model)
4. Set validation attributes HTML từ DataAnnotations (`required`, `maxlength`, v.v.)
5. Liên kết với `asp-validation-for="Name"` span

Không cần viết thủ công `name`, `id`, `value` — giảm lỗi và tăng type safety.

---

**Câu 18:** Tại sao `CheckoutController` cần `[Authorize]` nhưng `CartController` thì không?

**Đáp án:** Giỏ hàng (`Cart`) là **anonymous-friendly** — khách chưa đăng nhập vẫn được thêm sản phẩm vào giỏ để xem/so sánh. Session không cần authentication.

Checkout (`Checkout`) cần biết **ai** đang đặt hàng để:
1. Pre-fill thông tin từ user profile
2. Lưu `Order.UserName` để user xem lịch sử đơn sau này
3. Bảo vệ tài nguyên — đơn hàng cần gắn với tài khoản cụ thể

---

**Câu 19:** `SelectList` trong `PopulateCategories()` dùng như thế nào?

**Đáp án:**
```csharp
// ProductsController.cs:138
ViewBag.Categories = new SelectList(_categories.GetAllCategories(), "Id", "Name", selected);
```

`SelectList(items, valueField, textField, selectedValue)`:
- `items`: danh sách nguồn
- `"Id"`: field dùng làm value của option
- `"Name"`: field hiển thị text
- `selected`: value của option đang chọn

Trong View: `<select asp-for="CategoryId" asp-items="ViewBag.Categories">` — Tag Helper tự render `<option value="1">Laptop</option>` v.v.

---

**Câu 20:** Luồng đặt hàng (Checkout) từ A đến Z là gì?

**Đáp án:**
1. User click "Thanh toán" → `CheckoutController.Index GET`
2. Check `[Authorize]` — nếu chưa đăng nhập → redirect Login
3. Check giỏ trống → redirect Cart
4. Pre-fill form từ user profile
5. User điền địa chỉ, chọn thanh toán → POST `/Checkout/Confirm`
6. Server validate form (`ModelState.IsValid`)
7. Tạo `Order` object + map `CartItems → OrderDetails` (snapshot giá, tên)
8. `EFOrderRepository.Add(order)` — tạo `OrderCode` = `ORD{timestamp}{random}`, lưu PostgreSQL
9. `CartService.Clear()` — xoá Session
10. Redirect `Checkout/Success?code=ORD...`

---

## PHẦN 2: Câu hỏi Trung bình (20 câu)

---

**Câu 21:** EF Core là gì? Explain rõ luồng từ LINQ đến SQL đến C# object.

**Đáp án:** EF Core (Entity Framework Core) là ORM — Object-Relational Mapper. Developer viết LINQ, EF Core dịch sang SQL.

```csharp
// EFProductRepository.cs
_context.Products.ToList()
```
EF Core sinh ra: `SELECT "p"."Id", "p"."Name", ... FROM "Products" AS "p"`

Kết quả rows PostgreSQL → EF Core map sang `List<Product>` theo convention (tên column = tên property).

---

**Câu 22:** Tại sao EFProductRepository dùng `Scoped` thay vì `Singleton`?

**Đáp án:** `ApplicationDbContext` được đăng ký `Scoped` (thêm bởi `AddDbContext`). EF Core DbContext **không thread-safe** — không thể share giữa các requests.

Nếu Repository là `Singleton` nhưng inject `Scoped` DbContext → **captive dependency bug**: DbContext bị giữ sống lâu hơn intended, có thể dùng connection đã close hoặc data stale.

**Rule:** Service có dependency Scoped thì bản thân cũng phải Scoped (hoặc Transient).

---

**Câu 23:** `HasConversion` trong ApplicationDbContext dùng để làm gì?

**Đáp án:** Khi PostgreSQL không có kiểu `List<string>`, EF Core cần biết cách convert.

```csharp
// ApplicationDbContext.cs:28
.HasConversion(
    v => JsonSerializer.Serialize(v ?? new List<string>(), null),    // C# → DB: serialize JSON
    v => JsonSerializer.Deserialize<List<string>>(v, null) ?? new(), // DB → C#: deserialize
    new ValueComparer<List<string>>(...) // cách so sánh để detect changes
)
```

Khi EF Core đọc từ DB → deserialize JSON string → `List<string>`.
Khi EF Core ghi vào DB → serialize `List<string>` → JSON string.

---

**Câu 24:** Giải thích SessionExtensions và tại sao cần serialize/deserialize.

**Đáp án:** `ISession` chỉ hỗ trợ lưu `byte[]`, `int`, `string` — không lưu được object phức tạp như `ShoppingCart`.

```csharp
// Services/SessionExtensions.cs
public static void SetObject(this ISession session, string key, object value)
    => session.SetString(key, JsonSerializer.Serialize(value));

public static T? GetObject<T>(this ISession session, string key)
    => JsonSerializer.Deserialize<T>(session.GetString(key));
```

ShoppingCart được serialize thành JSON string để lưu, deserialize khi đọc về. Extension method giúp gọi gọn: `Session.SetObject("key", cart)`.

---

**Câu 25:** `ShopController.Index` dùng `.AsEnumerable()` thay vì `.AsQueryable()`. Sự khác biệt là gì và ảnh hưởng gì?

**Đáp án:**
- `.AsQueryable()`: Filter/sort xảy ra trong SQL (server-side) — chỉ lấy data cần thiết về
- `.AsEnumerable()`: Lấy toàn bộ data về RAM, filter bằng LINQ to Objects

```csharp
// ShopController.cs:21
var query = _products.GetAll().AsEnumerable();
```

Vì `GetAll()` đã `.ToList()` trong Repository, `AsEnumerable()` chỉ là marker để compiler dùng `IEnumerable<T>` extension methods. Toàn bộ filter xảy ra in-memory.

**Ảnh hưởng:** Với database nhỏ OK. Nếu có 100.000 sản phẩm → lấy toàn bộ về RAM mới filter → chậm. Giải pháp: Trả `IQueryable<Product>` từ Repository và filter trước khi `.ToList()`.

---

**Câu 26:** Tại sao `Update POST` trong `ProductsController` phải load `existing` product trước?

**Đáp án:**
```csharp
// ProductsController.cs:104
var existing = _products.GetById(product.Id);
// ...
product.Rating = existing.Rating;
product.ReviewCount = existing.ReviewCount;
product.Sold = existing.Sold;
product.CreatedAt = existing.CreatedAt;
```

Lý do: Form Update chỉ chứa các fields admin sửa (Name, Price...), không chứa Rating, Sold, CreatedAt. Nếu ghi thẳng object từ form → các fields này về 0/null.

Giải pháp: Load existing từ DB, copy các field cần bảo vệ từ existing sang object mới trước khi Update.

---

**Câu 27:** Tại sao file ảnh được đổi tên thành GUID thay vì giữ tên gốc?

**Đáp án:** Dùng `Guid.NewGuid():N` để đặt tên file ngẫu nhiên vì:

1. **Tránh trùng tên:** Hai user upload `product.jpg` sẽ overwrite nhau nếu giữ tên gốc
2. **Ngăn Path Traversal:** Nếu giữ tên gốc, user có thể upload file tên `../../web.config` → ghi đè file hệ thống
3. **Predictability:** Tên ngẫu nhiên khó đoán — khó bruteforce URL ảnh của người khác

---

**Câu 28:** Giải thích `OrderStatus` enum và cách lưu trong DB.

**Đáp án:**
```csharp
// Models/Order.cs:6
public enum OrderStatus { Pending=0, Confirmed=1, Shipping=2, Completed=3, Cancelled=4 }
```

EF Core lưu enum dưới dạng `integer` trong DB (theo migration: `Status integer NOT NULL`). Khi đọc, EF Core cast `int → OrderStatus` tự động.

Admin có thể cập nhật qua `OrdersController.UpdateStatus(int id, OrderStatus status)`.

---

**Câu 29:** Pagination trong ShopController hoạt động như thế nào? Tính `TotalPages` ra sao?

**Đáp án:**
```csharp
var total = query.Count();          // Đếm trước khi paginate
page = Math.Max(1, page);           // Trang tối thiểu là 1
var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
```

`TotalPages` là computed property trong ShopViewModel:
```csharp
// Models/ShopViewModel.cs:15
public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(PageSize, 1));
```

Ví dụ: 25 sản phẩm, pageSize=12 → TotalPages = Ceiling(25/12) = 3

---

**Câu 30:** Giải thích luồng xác thực password khi đăng nhập.

**Đáp án:**
```csharp
// AccountController.cs:39
var result = await _signInManager.PasswordSignInAsync(
    vm.UserName, vm.Password, vm.RememberMe, lockoutOnFailure: false);
```

Bên trong `PasswordSignInAsync`:
1. `UserManager.FindByNameAsync(userName)` → tìm user trong DB
2. Nếu không tìm thấy → fail
3. `UserManager.CheckPasswordAsync(user, password)`:
   - Lấy `user.PasswordHash` từ DB
   - Hash `vm.Password` với cùng salt (từ PasswordHash)
   - So sánh result
4. Nếu khớp → tạo ClaimsPrincipal với các claims (UserId, UserName, Roles...)
5. `HttpContext.SignInAsync()` → mã hóa claims vào cookie

---

**Câu 31:** Tại sao `WishlistService.Toggle` trả về `bool`? Dùng giá trị đó như thế nào?

**Đáp án:**
```csharp
// Services/WishlistService.cs:18
public bool Toggle(int productId)
{
    var ids = GetIds();
    var added = ids.Add(productId);  // HashSet.Add trả false nếu đã có
    if (!added) ids.Remove(productId);
    Session.SetObject(Key, ids);
    return added;  // true = vừa thêm, false = vừa xoá
}
```

Controller trả JSON `{ added, count }`. JavaScript dùng `added` để quyết định đổi icon từ `bi-heart` (rỗng) sang `bi-heart-fill` (đầy) hay ngược lại — UX realtime không cần reload.

---

**Câu 32:** `[NotMapped]` attribute làm gì? Tại sao `ApplicationUser.Role` dùng attribute này?

**Đáp án:** `[NotMapped]` báo EF Core **bỏ qua** property khi tạo/đọc/ghi database. Field này chỉ tồn tại trong RAM.

`ApplicationUser.Role` không lưu DB vì roles được quản lý qua bảng `AspNetUserRoles`. Controllers load role riêng khi cần:
```csharp
// AccountController.cs:106
user.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";
```

Nếu không có `[NotMapped]`, EF Core sẽ cố tạo cột `Role` trong bảng `AspNetUsers` → migration lỗi hoặc data không nhất quán.

---

**Câu 33:** Giải thích cơ chế "snapshot" trong OrderDetail. Tại sao cần lưu cả tên và giá sản phẩm?

**Đáp án:** Khi đặt hàng, Controller copy giá và tên từ CartItem sang OrderDetail:
```csharp
// CheckoutController.cs:74
Items = cart.Items.Select(i => new OrderDetail {
    ProductId = i.ProductId,
    ProductName = i.Name,    // snapshot
    Price = i.Price,          // snapshot
    Quantity = i.Quantity
}).ToList()
```

**Lý do:** Nếu admin sau đó sửa Product (đổi tên, giảm/tăng giá), lịch sử đơn hàng cũ vẫn giữ nguyên giá trị lúc đặt. Đây là **Event Sourcing** pattern đơn giản — bảo vệ integrity của dữ liệu lịch sử.

---

**Câu 34:** `ValueComparer` trong ApplicationDbContext dùng để làm gì?

**Đáp án:**
```csharp
// ApplicationDbContext.cs:31
new ValueComparer<List<string>>(
    (a, b) => a != null && b != null && a.SequenceEqual(b),  // So sánh bằng
    c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),  // GetHashCode
    c => c.ToList()  // Clone để tracking
)
```

EF Core Change Tracking cần so sánh giá trị cũ và mới để biết có thay đổi không. Với `List<string>`, so sánh reference (`==`) luôn false dù nội dung giống. `ValueComparer` định nghĩa cách so sánh **nội dung** (dùng `SequenceEqual`).

---

**Câu 35:** Middleware pipeline trong `Program.cs` có thứ tự như thế nào? Thứ tự có quan trọng không?

**Đáp án:** Thứ tự **cực kỳ quan trọng** — middleware chạy theo thứ tự đăng ký:

```csharp
app.UseStaticFiles();    // Phục vụ wwwroot trước, không cần auth
app.UseRouting();        // Phân tích URL → xác định controller/action
app.UseSession();        // Khởi tạo session (phải TRƯỚC auth để auth có thể dùng session nếu cần)
app.UseAuthentication(); // Đọc cookie, set HttpContext.User
app.UseAuthorization();  // Kiểm tra [Authorize] attributes
// MVC xử lý request ở đây
```

Nếu `UseAuthentication` trước `UseSession` → OK. Nhưng nếu đặt `UseAuthorization` trước `UseAuthentication` → `HttpContext.User` chưa được set → authorize luôn fail.

---

**Câu 36:** Giải thích `ShippingFee` logic trong `ShoppingCart`. Khi nào freeship?

**Đáp án:**
```csharp
// Models/Cart.cs:21
public decimal ShippingFee => Subtotal >= 500_000 || Subtotal == 0 ? 0 : 30_000;
```

- `Subtotal >= 500.000đ` → Freeship (thể hiện trong header: "Miễn phí vận chuyển cho đơn hàng từ 500.000đ")
- `Subtotal == 0` (giỏ trống) → 0 (không tính ship cho giỏ rỗng)
- `Subtotal < 500.000đ` và `> 0` → 30.000đ

---

**Câu 37:** Tại sao `_Layout.cshtml` inject service trực tiếp thay vì nhận qua ViewBag?

**Đáp án:**
```cshtml
@inject CartService CartSvc
@inject WishlistService WishlistSvc
@inject ICategoryRepository CatRepo
```

Layout xuất hiện trên **mọi trang** — nếu Controller phải truyền data qua ViewBag, **mọi** Controller phải thêm logic lấy cart, wishlist, categories. Điều này vi phạm DRY và dễ quên.

Với `@inject`, Layout tự lấy data cần thiết mà không cần Controller can thiệp — **separation of concerns** tốt hơn.

---

**Câu 38:** Tại sao `UsersController.Delete` check `user.UserName != "admin"` trước khi xoá?

**Đáp án:**
```csharp
// UsersController.cs:43
if (user is not null && user.UserName != "admin")
    await _userManager.DeleteAsync(user);
```

Nếu không có check này, admin có thể vô tình (hoặc bị CSRF) xoá account "admin" → mất hoàn toàn quyền truy cập hệ thống.

Đây là **safeguard** đơn giản. Production cần thêm: không cho xoá chính mình, require re-authentication trước khi xoá.

---

**Câu 39:** Giải thích `_ViewStart.cshtml` và `_ViewImports.cshtml`.

**Đáp án:**
- **`_ViewStart.cshtml`** (`Views/_ViewStart.cshtml`): Chạy trước mọi View, set Layout mặc định:
  ```cshtml
  @{ Layout = "_Layout"; }
  ```
- **`_ViewImports.cshtml`** (`Views/_ViewImports.cshtml`): Khai báo các `@using` và `@addTagHelper` dùng chung cho tất cả Views — không cần lặp lại trong từng file.

Admin Area có `_ViewStart.cshtml` riêng, set `Layout = "_AdminLayout"` → các Admin views tự động dùng layout Admin.

---

**Câu 40:** Explain `Chart.js` integration trong Dashboard. Dữ liệu đưa vào Chart như thế nào?

**Đáp án:** Controller tạo mảng dữ liệu C#, View serialize thành JS array:
```cshtml
// Dashboard/Index.cshtml:118
const labels = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.RevenueLabels));
const revSeries = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(Model.RevenueSeries));
```

`@Html.Raw` không escape HTML → JSON array render thẳng vào JavaScript, Chart.js nhận và vẽ.

Ví dụ output:
```javascript
const labels = ["23/05","24/05",...,"05/06"];
const revSeries = [0,5000000,...,12000000];
```

---

## PHẦN 3: Câu hỏi Nâng cao (20 câu)

---

**Câu 41:** Nếu cần scale hệ thống lên 10.000 concurrent users, điểm nào trong code cần thay đổi đầu tiên?

**Đáp án:**
1. **Session in-memory → Redis**: Hiện `DistributedMemoryCache` chỉ work trên 1 server. Multi-server cần Redis.
2. **`GetAll().AsEnumerable()` trong ShopController**: Đang load toàn bộ sản phẩm vào RAM rồi filter — cần chuyển sang IQueryable với WHERE, ORDER BY, LIMIT trực tiếp trong SQL.
3. **`_userManager.Users.Count()`** trong DashboardController: Gọi `COUNT(*)` mỗi request → cần cache.
4. **Database connection pool**: Tăng pool size cho Npgsql.
5. **CDN cho static files**: Ảnh sản phẩm hiện serve từ `wwwroot/images/` → cần chuyển lên S3/Cloudflare.

---

**Câu 42:** Giải thích tại sao `OrderCode` có unique index. Vấn đề gì có thể xảy ra nếu không có?

**Đáp án:**
```csharp
// ApplicationDbContext.cs:50
builder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
```

**Vấn đề nếu không có unique index:** Hai requests đồng thời tạo order cùng lúc có thể tạo cùng `OrderCode` (race condition). Unique index ở database level đảm bảo constraint ngay cả khi app không check.

`OrderCode` = `ORD{timestamp}{random3digits}` — timestamp giảm xác suất trùng, random 3 số thêm một lớp bảo vệ. Unique index là "last line of defense".

---

**Câu 43:** `SecurityStamp` trong IdentityUser là gì? Tại sao quan trọng?

**Đáp án:** `SecurityStamp` là GUID string, thay đổi mỗi khi:
- User đổi password
- User được gán/bỏ role
- User bị lock/unlock

Cookie authentication chứa `SecurityStamp` tại thời điểm login. Middleware định kỳ validate stamp từ cookie với stamp trong DB. Nếu không khớp → cookie bị vô hiệu → buộc đăng nhập lại.

**Tại sao cần:** Nếu admin ban user hoặc user đổi password, các session cũ của user đó bị vô hiệu ngay lập tức (tối đa trong khoảng thời gian check period).

---

**Câu 44:** Giải thích `Data Protection API` trong ASP.NET Core. Cookie có được mã hóa không?

**Đáp án:** ASP.NET Core Data Protection API cung cấp cryptographic operations. Cookie authentication sử dụng nó để:

1. **Mã hóa (encrypt):** Claims Principal → serialize → mã hóa AES-256-CBC
2. **Ký (sign):** HMAC-SHA256 để đảm bảo tính toàn vẹn

Cookie value chứa ciphertext, không phải plaintext claims. Kẻ tấn công đánh cắp cookie có thể replay (nên cần HTTPS + Secure flag), nhưng không đọc được nội dung.

Keys mặc định lưu trong `%APPDATA%\ASP.NET\DataProtection-Keys` (Windows) hoặc `~/.aspnet/DataProtection-Keys` (Linux). Production nên lưu keys trên Azure Key Vault/Redis.

---

**Câu 45:** LINQ `Where` trong ShopController dùng `ToLowerInvariant()`. Tại sao không dùng `ToLower()` hay `Contains` trực tiếp (case-sensitive)?

**Đáp án:**
```csharp
// ShopController.cs:29
var kw = keyword.Trim().ToLowerInvariant();
query = query.Where(p => p.Name.ToLowerInvariant().Contains(kw));
```

- `ToLower()` phụ thuộc vào `CultureInfo.CurrentCulture` → kết quả khác nhau tùy locale (tiếng Thổ Nhĩ Kỳ nổi tiếng có bug chữ I)
- `ToLowerInvariant()` dùng `InvariantCulture` — kết quả nhất quán trên mọi hệ thống

Vì đây là LINQ to Objects (đã `AsEnumerable()`), không thể dùng `EF.Functions.ILike` (PostgreSQL case-insensitive) — phải lowercase cả hai phía.

---

**Câu 46:** `MockProductRepository` và `EFProductRepository` cùng implement `IProductRepository`. Điều này minh họa SOLID principle nào?

**Đáp án:** **Open/Closed Principle (OCP)** và **Dependency Inversion Principle (DIP)**:

- **OCP:** `IProductRepository` interface đóng với modification, mở cho extension — thêm `EFProductRepository` không sửa interface
- **DIP:** Controllers depend on `IProductRepository` abstraction, không phụ thuộc vào concrete class

Cũng minh họa **Liskov Substitution Principle (LSP):** `EFProductRepository` có thể thay thế `MockProductRepository` mà không làm hỏng Controller code.

Để swap implementation, chỉ cần sửa 1 dòng trong `Program.cs`:
```csharp
// Từ Mock
builder.Services.AddScoped<IProductRepository, MockProductRepository>();
// Sang EF
builder.Services.AddScoped<IProductRepository, EFProductRepository>();
```

---

**Câu 47:** Giải thích tại sao `ProductController.Review` phải cập nhật `Product.Rating` sau khi thêm review mới.

**Đáp án:**
```csharp
// ProductController.cs:50
var all = _reviews.GetByProduct(review.ProductId).ToList();
var p = _products.GetById(review.ProductId);
if (p is not null && all.Count > 0)
{
    p.Rating = Math.Round(all.Average(r => r.Rating), 1);
    p.ReviewCount = all.Count;
    _products.Update(p);
}
```

`Product.Rating` là **denormalized aggregate** — lưu sẵn để tránh tính lại mỗi lần hiển thị. Nếu không cập nhật, Rating cũ sẽ không phản ánh review mới.

**Trade-off:** Nếu xoá review (qua Admin), phải cập nhật lại Rating. Hiện tại `ReviewsController.Delete` không làm điều này → **inconsistency bug tiềm năng**.

---

**Câu 48:** Tại sao cần `DistributedMemoryCache` thay vì chỉ dùng Session thông thường?

**Đáp án:**
```csharp
// Program.cs:52
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opt => { ... });
```

ASP.NET Core Session yêu cầu **`IDistributedCache`** backend để lưu session data. `AddDistributedMemoryCache()` cung cấp in-process implementation.

Nếu không `AddDistributedMemoryCache()`, `AddSession()` sẽ throw exception vì không tìm thấy `IDistributedCache` trong DI container.

**Thay thế cho production:** `builder.Services.AddStackExchangeRedisCache(...)` → session data lưu trong Redis, survive server restart, work với multiple instances.

---

**Câu 49:** Giải thích cơ chế Areas trong ASP.NET Core. Tại sao Admin UI dùng Area?

**Đáp án:** Areas là cách **phân vùng** ứng dụng lớn thành các module độc lập, mỗi Area có Controllers, Views, Models riêng.

URL pattern: `{area}/{controller}/{action}/{id?}` — khai báo trong `Program.cs:80`.

`[Area("Admin")]` trên controller báo framework controller này thuộc Area "Admin", tìm view trong `Areas/Admin/Views/`.

**Lý do dùng Area cho Admin:**
1. Tách biệt hoàn toàn với customer-facing code
2. Layout riêng (`_AdminLayout.cshtml`) — sidebar, topbar khác
3. Route riêng (`/Admin/...`) — không conflict với customer routes
4. Dễ áp dụng policy riêng (`[Authorize(Roles="Admin")]` ở Area level)

---

**Câu 50:** `Npgsql.EntityFrameworkCore.PostgreSQL` làm gì? Nếu muốn chuyển sang SQL Server thì cần thay đổi gì?

**Đáp án:** Npgsql là **EF Core Database Provider** cho PostgreSQL — dịch LINQ → PostgreSQL-specific SQL, quản lý connection pool, xử lý PostgreSQL-specific types.

**Để chuyển sang SQL Server:**
1. Xoá `Npgsql.EntityFrameworkCore.PostgreSQL`, thêm `Microsoft.EntityFrameworkCore.SqlServer`
2. Trong `Program.cs`: `options.UseNpgsql(...)` → `options.UseSqlServer(...)`
3. Xoá toàn bộ Migrations (SQL syntax khác nhau giữa 2 DBMS)
4. Chạy lại `dotnet ef migrations add Initial`
5. Cập nhật connection string trong `appsettings.json`

Toàn bộ code khác (Repository, Controller, Model) **không cần thay đổi** — đây là ưu điểm của EF Core abstraction.

---

**Câu 51:** Giải thích `Razor Runtime Compilation`. Tại sao dùng trong dev nhưng tắt trong production?

**Đáp án:**
```xml
<!-- WebBanHang_Bai2.csproj -->
<PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="8.0.0" />
```
```csharp
// Program.cs:43
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
```

Mặc định, Views được compile tại build time (AOT). Runtime Compilation cho phép sửa `.cshtml` mà không cần `dotnet build` — thấy ngay hiệu quả khi refresh browser.

**Dev:** Hữu ích — tăng tốc iteration loop.
**Production:** Tắt — mỗi request có thể trigger recompile → latency cao, memory overhead. Production nên compile trước và publish binary.

---

**Câu 52:** `IWebHostEnvironment` trong `ProductsController` dùng để làm gì? Tại sao cần inject?

**Đáp án:**
```csharp
// ProductsController.cs:16
private readonly IWebHostEnvironment _env;
// ...
var dir = Path.Combine(_env.WebRootPath, "images");
```

`IWebHostEnvironment.WebRootPath` trả về đường dẫn tuyệt đối đến `wwwroot/` — khác nhau trên mỗi máy và môi trường deploy.

Ví dụ:
- Dev: `/home/khanh/TH_LapTrinhWeb/Bài 2/WebBanHang_Bai2/wwwroot`
- Production: `/var/www/techstore/wwwroot`

Inject `IWebHostEnvironment` thay vì hardcode path → code portable giữa các môi trường.

---

**Câu 53:** Giải thích `ClaimsPrincipal` trong ASP.NET Core Identity. Làm sao biết user có role Admin trong code?

**Đáp án:** Sau đăng nhập, Identity tạo `ClaimsPrincipal` với các `Claim`:
- `ClaimTypes.NameIdentifier` = user.Id
- `ClaimTypes.Name` = user.UserName
- `ClaimTypes.Role` = "Admin" (hoặc "Customer")

Trong Controller/View có thể check:
```csharp
User.Identity?.IsAuthenticated          // bool
User.Identity?.Name                     // "admin"
User.IsInRole("Admin")                  // bool
User.FindFirst(ClaimTypes.NameIdentifier)?.Value  // userId
```

Trong `_Layout.cshtml`:
```cshtml
@if (User.IsInRole("Admin")) { <a>Admin Dashboard</a> }
```

---

**Câu 54:** Tại sao `CheckoutController.Confirm` không check stock (tồn kho) trước khi đặt hàng?

**Đáp án:** Đây là **limitation** của implementation hiện tại. Không có logic:
```csharp
if (product.Stock < item.Quantity) { TempData["Error"] = "Hàng tồn kho không đủ"; ... }
```

**Cách fix:**
1. Trong `CheckoutController.Confirm`, load từng Product và kiểm tra `Stock >= item.Quantity`
2. Nếu OK, trừ Stock: `product.Stock -= item.Quantity; _products.Update(product);`
3. Cần transaction để tránh race condition (2 user cùng mua sản phẩm cuối cùng)

Đây là bài tập tốt để sinh viên thêm vào.

---

**Câu 55:** `Optimistic Concurrency` là gì? `ConcurrencyStamp` trong Identity liên quan gì?

**Đáp án:** Optimistic Concurrency: Giả định ít conflict xảy ra → không lock record khi đọc, chỉ verify khi write.

Identity table `AspNetUsers` có `ConcurrencyStamp` (GUID). Khi update user:
1. EF Core gửi: `WHERE Id = @id AND ConcurrencyStamp = @originalStamp`
2. Nếu ai đó sửa user trước → stamp thay đổi → WHERE clause không match → `DbUpdateConcurrencyException`

Prevents "last write wins" problem khi 2 admin cùng sửa user profile.

---

**Câu 56:** Giải thích pattern `@(User.Identity?.Name?.Substring(0, 1).ToUpper() ?? "U")` trong Layout.

**Đáp án:**
```cshtml
// _Layout.cshtml:69
<span class="avatar">@(User.Identity.Name?.Substring(0, 1).ToUpper() ?? "U")</span>
```

1. `User.Identity.Name` — tên user (có thể null nếu anonymous)
2. `?.Substring(0, 1)` — lấy ký tự đầu (null-safe với `?.`)
3. `.ToUpper()` — in hoa → avatar chữ cái đầu
4. `?? "U"` — fallback về "U" nếu null

Tạo avatar text từ chữ cái đầu của username — pattern phổ biến khi không có ảnh avatar.

---

**Câu 57:** Tại sao cần `Include(o => o.Items)` trong EFOrderRepository?

**Đáp án:**
```csharp
// EFOrderRepository.cs:13
public IEnumerable<Order> GetAll() =>
    _context.Orders.Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToList();
```

EF Core lazy loading mặc định tắt trong .NET 8. Không có `Include`, `order.Items` sẽ là collection rỗng dù DB có data.

`Include` → EF Core tạo JOIN query:
```sql
SELECT o.*, od.* FROM "Orders" o
LEFT JOIN "OrderDetails" od ON od."OrderId" = o."Id"
ORDER BY o."CreatedAt" DESC
```

**`OwnsMany` vẫn cần Include:** Dù OrderDetail là owned entity, EF Core vẫn cần explicit `.Include` để load related data.

---

**Câu 58:** `FormOptions.MultipartBodyLengthLimit` và `Kestrel MaxRequestBodySize` khác nhau như thế nào? Tại sao cần set cả 2?

**Đáp án:**
```csharp
// Program.cs:39
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 104_857_600);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 104_857_600);
```

- **`MaxRequestBodySize` (Kestrel):** Giới hạn ở tầng HTTP server — Kestrel reject request vượt quá trước khi đọc body
- **`MultipartBodyLengthLimit` (FormOptions):** Giới hạn ở tầng model binding — sau khi Kestrel đọc body, MVC form parser kiểm tra

Cần set cả 2 vì Kestrel mặc định limit nhỏ hơn (30MB). Nếu chỉ set FormOptions mà Kestrel vẫn limit nhỏ → request bị reject ở tầng dưới.

---

**Câu 59:** Nếu thêm chức năng "Quên mật khẩu" (Forgot Password), cần thêm những gì?

**Đáp án:**

1. **Email service:** Cần SMTP/SendGrid để gửi email reset link. Thêm `IEmailSender` service.
2. **Action GET/POST ForgotPassword:** User nhập email → tạo token reset.
3. **`UserManager.GeneratePasswordResetTokenAsync(user)`** — Identity tạo secure token (stored token → `AspNetUserTokens`)
4. **Action GET/POST ResetPassword:** User click link → verify token → đặt password mới.
5. **`UserManager.ResetPasswordAsync(user, token, newPassword)`** — verify token, hash password mới.

**Link trong Login.cshtml:**
```html
<a href="#">Quên mật khẩu?</a>  <!-- hiện tại là link rỗng -->
```

---

**Câu 60:** Nếu muốn cho phép đặt hàng không cần đăng nhập (guest checkout), cần thay đổi gì trong `CheckoutController`?

**Đáp án:**

**Thay đổi cần thiết:**
1. Bỏ `[Authorize]` ở class level (hoặc chỉ giữ ở method cụ thể)
2. Không pre-fill form từ user profile (vì có thể null)
3. `Order.UserName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;`
4. Không cần `UserManager` inject nữa (hoặc giữ nhưng optional)

**Thách thức:**
- Guest không xem được lịch sử đơn hàng (cần email confirmation thay thế)
- Cần validate email format chắc chắn vì đây là điểm liên lạc duy nhất

```csharp
// Thay vì:
var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
var vm = new CheckoutViewModel { CustomerName = user?.FullName ?? "" };

// Dùng:
var vm = new CheckoutViewModel();
if (User.Identity?.IsAuthenticated == true) {
    var user = await _userManager.FindByNameAsync(User.Identity.Name ?? "");
    vm.CustomerName = user?.FullName ?? "";
    // pre-fill...
}
```
