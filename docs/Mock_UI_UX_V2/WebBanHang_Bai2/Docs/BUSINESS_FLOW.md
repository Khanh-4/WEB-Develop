# BUSINESS FLOW — Phân tích nghiệp vụ TechStore

> Phân tích 6 luồng chính của hệ thống, có sơ đồ Mermaid cho từng luồng.

---

## Luồng 1: Đăng ký / Đăng nhập / Đăng xuất

### 1A. Đăng ký tài khoản mới

**Files liên quan:**
- `Controllers/AccountController.cs` — `Register GET/POST`
- `Views/Account/Register.cshtml`
- `Models/AppUser.cs` — `RegisterViewModel`
- `Data/DbSeeder.cs` — seed roles

```mermaid
flowchart TD
    A([User điền form Register]) --> B{ModelState.IsValid?}
    B -->|Không| C[Trả lại View với lỗi validation\ne.g. mật khẩu < 6 ký tự]
    B -->|Có| D[UserManager.CreateAsync\nHash password PBKDF2]
    D --> E{Tạo user thành công?}
    E -->|Lỗi\ne.g. UserName đã tồn tại| F[AddModelError + View]
    E -->|OK| G[AddToRoleAsync user, Customer]
    G --> H[TempData.Success = Đăng ký thành công]
    H --> I[RedirectToAction Login]
```

**Chi tiết:**
1. User POST `RegisterViewModel` gồm: FullName, UserName, Email, Password, ConfirmPassword
2. Server validate: `[Required]`, `[EmailAddress]`, `[StringLength]`, `[Compare]`
3. `UserManager.CreateAsync(user, password)` — Identity **tự hash password** bằng PBKDF2 với salt ngẫu nhiên → lưu vào `AspNetUsers.PasswordHash`
4. Gán role "Customer" qua `AspNetUserRoles`
5. Redirect về Login — **chưa auto-login** sau đăng ký

---

### 1B. Đăng nhập

```mermaid
flowchart TD
    A([User nhập UserName + Password]) --> B{ModelState.IsValid?}
    B -->|Không| C[View với lỗi]
    B -->|Có| D[SignInManager.PasswordSignInAsync\nxác minh password hash]
    D --> E{Kết quả?}
    E -->|Thất bại| F[ModelState.AddModelError\nTên đăng nhập hoặc mật khẩu không đúng]
    E -->|Thành công| G[Tạo Authentication Cookie\nTechStore.Auth]
    G --> H{Có returnUrl?}
    H -->|Có và là local URL| I[Redirect returnUrl]
    H -->|Không| J{User là Admin?}
    J -->|Có| K[Redirect /Admin/Dashboard]
    J -->|Không| L[Redirect /Home/Index]
```

**Cookie Authentication:**
- Cookie name: `TechStore.Auth` (`Program.cs:35`)
- Expire: 7 ngày (`ExpireTimeSpan = TimeSpan.FromDays(7)`)
- `SlidingExpiration = true`: mỗi request gia hạn thêm 7 ngày
- `HttpOnly = true` (mặc định): JavaScript không đọc được cookie

**`returnUrl` security:** `Url.IsLocalUrl(returnUrl)` kiểm tra URL là local (cùng domain) trước khi redirect — chống **Open Redirect Attack**.

---

### 1C. Đăng xuất

```mermaid
flowchart TD
    A([User click Đăng xuất]) --> B[POST /Account/Logout\nValidateAntiForgeryToken]
    B --> C[SignInManager.SignOutAsync\nXoá Authentication Cookie]
    C --> D[TempData.Success = Đã đăng xuất]
    D --> E[Redirect /Home/Index]
```

**Tại sao Logout phải POST?** Nếu dùng GET, kẻ tấn công có thể nhúng `<img src="/Account/Logout">` vào trang khác → CSRF logout. POST + CSRF token ngăn chặn điều này.

---

## Luồng 2: Xem danh sách sản phẩm và tìm kiếm/lọc

**Files liên quan:**
- `Controllers/ShopController.cs`
- `Views/Shop/Index.cshtml`
- `Models/ShopViewModel.cs`

```mermaid
flowchart TD
    A([Browser GET /Shop]) --> B[ShopController.Index\ncategoryId keyword sort minPrice maxPrice page pageSize]
    B --> C[_products.GetAll\nLấy tất cả từ DB]
    C --> D{categoryId có giá trị?}
    D -->|Có| E[Filter: p.CategoryId == categoryId]
    D -->|Không| F[Giữ nguyên]
    E --> G
    F --> G{keyword có giá trị?}
    G -->|Có| H[Filter: Name/Category/ShortDesc contains keyword]
    G -->|Không| I[Giữ nguyên]
    H --> J
    I --> J{minPrice/maxPrice?}
    J --> K[Filter theo khoảng giá]
    K --> L{sort}
    L --> M[price_asc/price_desc/name_asc/name_desc/rating/hot/newest]
    M --> N[Count tổng = TotalItems]
    N --> O[Skip page-1 x pageSize\nTake pageSize]
    O --> P[Build ShopViewModel]
    P --> Q[View Shop/Index.cshtml]
```

**Pagination logic:**
```
page=1, pageSize=12: Skip(0).Take(12) → items 1-12
page=2, pageSize=12: Skip(12).Take(12) → items 13-24
TotalPages = Math.Ceiling(TotalItems / 12)
```

**URL ví dụ:**
```
/Shop?categoryId=1&keyword=gaming&sort=price_asc&minPrice=20000000&maxPrice=60000000&page=2
```

---

## Luồng 3: Xem chi tiết sản phẩm

**Files liên quan:**
- `Controllers/ProductController.cs`
- `Views/Product/Detail.cshtml`
- `Models/ShopViewModel.cs` — `ProductDetailViewModel`

```mermaid
flowchart TD
    A([User click vào sản phẩm]) --> B[GET /Product/Detail/id]
    B --> C[_products.GetById id]
    C --> D{Product tồn tại?}
    D -->|Không| E[404 NotFound]
    D -->|Có| F[Lấy SP liên quan\ncùng CategoryId, loại SP này\nTake 4]
    F --> G[_reviews.GetByProduct id]
    G --> H[Build ProductDetailViewModel]
    H --> I[View Product/Detail.cshtml]
    I --> J[Render: Gallery ảnh + Info + Tabs]
    J --> K{User click tab Reviews?}
    K --> L[Hiện danh sách review + Form gửi đánh giá]
```

**Gallery ảnh:** Kết hợp `Product.ImageUrl` (ảnh chính) + `Product.ImageUrls` (danh sách ảnh phụ). Click thumbnail → đổi ảnh chính. Hover → zoom 1.6x.

---

## Luồng 4: Thêm vào giỏ hàng và quản lý giỏ

**Files liên quan:**
- `Controllers/CartController.cs`
- `Services/CartService.cs`
- `Services/SessionExtensions.cs`
- `Views/Cart/Index.cshtml`
- `Views/Shared/_Layout.cshtml` (cart drawer)

```mermaid
flowchart TD
    A([User click Thêm vào giỏ]) --> B[AJAX POST /Cart/Add\nproductId qty]
    B --> C[CartService.Add productId qty]
    C --> D[_products.GetById productId]
    D --> E{SP tồn tại?}
    E -->|Không| F[throw InvalidOperationException\nReturn 400 BadRequest]
    E -->|Có| G[Session.GetObject ShoppingCart\nParse JSON từ Session]
    G --> H{SP đã trong giỏ?}
    H -->|Có| I[existing.Quantity += qty]
    H -->|Không| J[cart.Items.Add new CartItem]
    I --> K[Session.SetObject cart\nSerialize JSON vào Session]
    J --> K
    K --> L[Return JsonResult cart]
    L --> M[JavaScript cập nhật UI\ncart drawer + badge count]
```

**Session flow chi tiết:**
```
Browser Request → Server
  ↓
Session middleware đọc Cookie "TechStore.Session"
  ↓
Tìm session data trong DistributedMemoryCache
  ↓
CartService.GetCart() gọi Session.GetObject<ShoppingCart>
  ↓
SessionExtensions.GetObject: Session.GetString(key) → JSON string
  ↓
JsonSerializer.Deserialize<ShoppingCart>(json) → ShoppingCart object
  ↓
Thêm/sửa CartItem
  ↓
SessionExtensions.SetObject: JsonSerializer.Serialize(cart) → JSON string
Session.SetString(key, json) → lưu lại
```

**Phí vận chuyển logic (Models/Cart.cs:21):**
```csharp
public decimal ShippingFee => Subtotal >= 500_000 || Subtotal == 0 ? 0 : 30_000;
```
- Subtotal >= 500K → Miễn phí
- Subtotal = 0 (giỏ trống) → 0
- Subtotal < 500K → 30.000đ

**Quản lý giỏ hàng (trang Cart/Index):**

```mermaid
flowchart LR
    A[Trang Cart/Index] --> B{Giỏ trống?}
    B -->|Có| C[Hiện Empty State + link về Shop]
    B -->|Không| D[Render bảng sản phẩm]
    D --> E[User click +/-]
    E --> F[AJAX POST /Cart/Update\nproductId qty]
    F --> G[CartService.Update\nqty<=0 → Remove\nqty>0 → Update]
    G --> H[JSON response]
    H --> I[Update UI: line total, subtotal, shipping, total]
    D --> J[User click Trash]
    J --> K[AJAX POST /Cart/Update qty=0]
    K --> G
```

---

## Luồng 5: Đặt hàng (Checkout)

**Files liên quan:**
- `Controllers/CheckoutController.cs` — `[Authorize]`
- `Views/Checkout/Index.cshtml`, `Success.cshtml`
- `Models/Order.cs` — `CheckoutViewModel`, `Order`, `OrderDetail`
- `Repositories/EFOrderRepository.cs`

```mermaid
flowchart TD
    A([User click Thanh toán ngay]) --> B{Đã đăng nhập?}
    B -->|Chưa| C[Redirect /Account/Login?returnUrl=/Checkout]
    B -->|Rồi| D[CheckoutController.Index GET]
    D --> E{Giỏ hàng trống?}
    E -->|Có| F[TempData.Error + Redirect Cart]
    E -->|Không| G[Load thông tin user\nPre-fill CheckoutViewModel]
    G --> H[ViewBag.Cart = cart]
    H --> I[View Checkout/Index.cshtml\nForm + Order Summary]
    I --> J[User điền form + chọn thanh toán]
    J --> K[POST /Checkout/Confirm]
    K --> L{ModelState.IsValid?}
    L -->|Không| M[View với lỗi validation]
    L -->|Có| N[Tạo Order object từ form]
    N --> O[Map CartItems → OrderDetails\nSnapshot giá + tên]
    O --> P[EFOrderRepository.Add order\nTạo OrderCode, SaveChanges]
    P --> Q[CartService.Clear\nXoá giỏ khỏi Session]
    Q --> R[TempData.Success + Redirect Success?code=ORD...]
    R --> S[CheckoutController.Success GET]
    S --> T[_orders.GetByCode code]
    T --> U[View Checkout/Success.cshtml]
```

**OrderCode generation (EFOrderRepository.cs:31):**
```csharp
order.OrderCode = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
```
Format: `ORD20260605130045123` — Timestamp + random 3 chữ số → đảm bảo unique.

**Phương thức thanh toán:** Chỉ lưu code (COD/VNPay/Momo/BankTransfer) vào DB. Chưa tích hợp payment gateway thực tế — đây là MVP.

---

## Luồng 6: Admin quản lý sản phẩm (CRUD)

**Files liên quan:**
- `Areas/Admin/Controllers/ProductsController.cs` — `[Authorize(Roles = "Admin")]`
- `Areas/Admin/Views/Products/` — Index, Add, Update
- `Repositories/EFProductRepository.cs`

```mermaid
flowchart TD
    A([Admin vào /Admin/Products]) --> B{Đã đăng nhập + Role Admin?}
    B -->|Không| C[Redirect /Account/Login hoặc AccessDenied]
    B -->|Có| D[ProductsController.Index\nDanh sách SP + filter + phân trang]

    D --> E{Admin chọn hành động}
    
    E -->|Thêm mới| F[GET /Admin/Products/Add]
    F --> G[Form trống + dropdown Category]
    G --> H[POST + upload ảnh]
    H --> I{ModelState valid?}
    I -->|Không| J[View với lỗi]
    I -->|Có| K[SaveImage → GUID.ext trong wwwroot/images]
    K --> L[product.Category = category.Name]
    L --> M[EFProductRepository.Add\nAutoSlug + IsNew=true + SaveChanges]
    M --> N[TempData.Success + Redirect Index]

    E -->|Sửa| O[GET /Admin/Products/Update/id]
    O --> P[Load existing product]
    P --> Q[Form pre-filled]
    Q --> R[POST + optional upload ảnh mới]
    R --> S{ModelState valid?}
    S -->|Không| T[View với lỗi]
    S -->|Có| U[Giữ Rating/ReviewCount/Sold/CreatedAt]
    U --> V[Append ảnh mới vào ImageUrls cũ]
    V --> W[EFProductRepository.Update\nSaveChanges]
    W --> X[TempData.Success + Redirect Index]

    E -->|Xoá| Y[POST /Admin/Products/Delete/id]
    Y --> Z[EFProductRepository.Delete\nSaveChanges]
    Z --> AA[TempData.Success + Redirect Index]
```

**Slug tự động (EFProductRepository.cs:44):**
```csharp
private static string Slugify(string s) =>
    new string(s.ToLowerInvariant()
        .Replace('đ', 'd')
        .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
    .Trim('-');
```
Ví dụ: "Laptop Dell XPS 13 Plus" → `"laptop-dell-xps-13-plus"`

**Upload ảnh flow:**
```
IFormFile → SaveImage() → 
Tạo GUID filename (e.g. "a3f7b2c1.jpg") → 
Lưu vào wwwroot/images/ → 
Return "/images/a3f7b2c1.jpg" (web path)
```

**Bảo vệ data khi Update:** Admin chỉ sửa được tên, giá, mô tả, ảnh. `Rating`, `ReviewCount`, `Sold`, `CreatedAt` được lấy từ record cũ để không bị reset về 0.

---

## Tổng hợp — Luồng phân quyền

```mermaid
flowchart TD
    REQUEST[HTTP Request] --> AUTH{Identity Middleware\nXác thực cookie}
    AUTH -->|Cookie hợp lệ| AUTHED[User đã xác thực]
    AUTH -->|Không có cookie| ANON[User ẩn danh]

    AUTHED --> ROLE{Kiểm tra Role}
    ROLE -->|Admin| ADMIN_AREA[Truy cập Admin Area\nAll Features]
    ROLE -->|Customer| CUSTOMER[Customer Features\nCheckout, Profile, Orders]

    ANON --> PUBLIC[Public Features\nShop, Product Detail, Cart, Wishlist]
    ANON --> CHECKOUT_ATTEMPT[Cố truy cập Checkout]
    CHECKOUT_ATTEMPT --> REDIRECT_LOGIN[Redirect Login?returnUrl=/Checkout]

    CUSTOMER --> ADMIN_ATTEMPT[Cố truy cập Admin]
    ADMIN_ATTEMPT --> ACCESS_DENIED[403 AccessDenied\n/Account/AccessDenied]
```
