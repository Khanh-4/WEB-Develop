# API ANALYSIS — TechStore

> Source: `Controllers/CartController.cs`, `Controllers/WishlistController.cs`

---

## Trạng thái hiện tại

Project **chưa có RESTful API** theo nghĩa chuẩn (Bài 6 chưa làm). Tuy nhiên, có một số MVC Action Methods trả về **JSON response** thay vì View — chủ yếu phục vụ AJAX calls từ JavaScript trong các file `.cshtml`.

**Điểm khác biệt MVC JSON Actions vs REST API:**

| Tiêu chí | MVC JSON Action (hiện tại) | REST API chuẩn (Bài 6) |
|---|---|---|
| Controller base | `Controller` (có View support) | `ControllerBase` (thuần API) |
| Route | `/Cart/Add`, `/Cart/Update` | `/api/cart/items` |
| Authentication | Cookie (Identity) | JWT Bearer Token |
| Anti-forgery | `[ValidateAntiForgeryToken]` (CSRF token) | Không cần (stateless) |
| Response format | `JsonResult` (ad-hoc) | `ActionResult<T>` với chuẩn hóa |
| Documentation | Không có Swagger | Swagger/OpenAPI |
| HTTP verbs | Chủ yếu POST | Đúng chuẩn GET/POST/PUT/DELETE |

---

## Các Endpoint JSON hiện có

### Group 1: CartController (`Controllers/CartController.cs`)

**Base URL:** `/Cart`

---

#### GET `/Cart/Json`

**Mục đích:** Lấy giỏ hàng hiện tại (dùng cho cart drawer ở header)

**Request:**
```
GET /Cart/Json
Cookie: TechStore.Session=...
```

**Response (HTTP 200):**
```json
{
  "items": [
    {
      "productId": 8,
      "name": "Chuột Logitech MX Master 3S",
      "price": 2590000,
      "quantity": 2,
      "imageUrl": "/images/mouse-1.svg",
      "lineTotal": 5180000
    }
  ],
  "totalQuantity": 2,
  "subtotal": 5180000,
  "shippingFee": 0,
  "total": 5180000
}
```

**Ghi chú:** JSON key tự động camelCase do ASP.NET Core default serializer settings. `ShippingFee = 0` vì `Subtotal >= 500.000đ`.

---

#### POST `/Cart/Add`

**Mục đích:** Thêm sản phẩm vào giỏ hàng

**Request:**
```
POST /Cart/Add
Content-Type: application/x-www-form-urlencoded
Cookie: TechStore.Session=...

productId=8&quantity=1&__RequestVerificationToken=<CSRF_TOKEN>
```

**Tham số:**
| Tên | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `productId` | int | Có | ID sản phẩm |
| `quantity` | int | Không (default=1) | Số lượng thêm |
| `__RequestVerificationToken` | string | Có | CSRF token từ form |

**Response thành công (HTTP 200):**
```json
{
  "items": [...],
  "totalQuantity": 3,
  "subtotal": 7770000,
  "shippingFee": 0,
  "total": 7770000
}
```

**Response lỗi (HTTP 400 Bad Request):**
```
"Sản phẩm không tồn tại."
```

**Logic:** Nếu sản phẩm đã có trong giỏ → cộng thêm quantity. Nếu chưa có → thêm item mới.

---

#### POST `/Cart/Update`

**Mục đích:** Cập nhật số lượng sản phẩm trong giỏ

**Request:**
```
POST /Cart/Update
Content-Type: application/x-www-form-urlencoded

productId=8&quantity=3&__RequestVerificationToken=<TOKEN>
```

**Tham số:**
| Tên | Kiểu | Mô tả |
|---|---|---|
| `productId` | int | ID sản phẩm cần cập nhật |
| `quantity` | int | Số lượng mới. Nếu <= 0 → xoá item |

**Response (HTTP 200):** ShoppingCart JSON (giống `/Cart/Json`)

**Logic:**
- `quantity <= 0` → `Items.RemoveAll(i => i.ProductId == productId)`
- `quantity > 0` → `existing.Quantity = quantity`

---

#### POST `/Cart/Remove`

**Mục đích:** Xoá sản phẩm khỏi giỏ

**Request:**
```
POST /Cart/Remove
productId=8&__RequestVerificationToken=<TOKEN>
```

**Response (HTTP 200):** ShoppingCart JSON sau khi xoá

---

#### POST `/Cart/Clear`

**Mục đích:** Xoá toàn bộ giỏ hàng

**Request:**
```
POST /Cart/Clear
__RequestVerificationToken=<TOKEN>
```

**Response (HTTP 200):**
```json
{
  "items": [],
  "totalQuantity": 0,
  "subtotal": 0,
  "shippingFee": 0,
  "total": 0
}
```

---

### Group 2: WishlistController (`Controllers/WishlistController.cs`)

**Base URL:** `/Wishlist`

---

#### POST `/Wishlist/Toggle`

**Mục đích:** Thêm vào / xoá khỏi danh sách yêu thích (toggle)

**Request:**
```
POST /Wishlist/Toggle
productId=8&__RequestVerificationToken=<TOKEN>
```

**Response (HTTP 200):**
```json
{
  "added": true,
  "count": 3
}
```

- `added: true` — vừa thêm vào wishlist
- `added: false` — vừa xoá khỏi wishlist
- `count` — tổng số item trong wishlist hiện tại

**JavaScript xử lý response:**
```javascript
// site.js (ngầm hiểu từ context)
const { added, count } = await response.json();
// Đổi màu heart icon
heartIcon.classList.toggle('bi-heart-fill', added);
heartIcon.classList.toggle('bi-heart', !added);
// Cập nhật badge count
document.getElementById('wishlistCount').textContent = count;
```

---

## Cách JavaScript gọi các endpoints này

Từ `Views/Cart/Index.cshtml` — ví dụ thực tế:

```javascript
async function syncRow(tr, qty) {
    const id = parseInt(tr.dataset.id, 10);
    const r = await fetch('/Cart/Update', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: new URLSearchParams({
            productId: id,
            quantity: qty,
            __RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
        })
    });
    if (!r.ok) return;
    const cart = await r.json();
    // Cập nhật UI...
}
```

**Lấy CSRF token từ đâu?** Token được render vào HTML qua `@Html.AntiForgeryToken()` hoặc `@Html.AntiForgeryToken()` trong form. JavaScript đọc từ DOM:
```javascript
document.querySelector('input[name="__RequestVerificationToken"]').value
```

---

## Gợi ý cho Bài 6 — RESTful API

Để hoàn thiện theo yêu cầu giáo trình CMP376 Bài 6, cần thêm:

### 1. Tạo API Controller riêng

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsApiController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Product>> GetAll() { ... }

    [HttpGet("{id}")]
    public ActionResult<Product> GetById(int id) { ... }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public ActionResult<Product> Create(Product product) { ... }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Update(int id, Product product) { ... }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id) { ... }
}
```

### 2. Đổi Authentication sang JWT Bearer

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });
```

### 3. Thêm Swagger/OpenAPI

```csharp
builder.Services.AddSwaggerGen();
// ...
app.UseSwagger();
app.UseSwaggerUI();
```

### 4. Các endpoints cần xây dựng

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/products` | Lấy danh sách + filter + phân trang |
| GET | `/api/products/{id}` | Chi tiết sản phẩm |
| POST | `/api/products` | Thêm sản phẩm (Admin) |
| PUT | `/api/products/{id}` | Cập nhật (Admin) |
| DELETE | `/api/products/{id}` | Xoá (Admin) |
| GET | `/api/categories` | Danh sách danh mục |
| POST | `/api/auth/login` | Đăng nhập, trả JWT |
| POST | `/api/auth/register` | Đăng ký |
| GET | `/api/orders` | Lịch sử đơn (Customer) / Tất cả (Admin) |
| POST | `/api/orders` | Đặt hàng |
| PATCH | `/api/orders/{id}/status` | Cập nhật trạng thái (Admin) |

### 5. Standardized Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Tên sản phẩm không được để trống"],
    "Price": ["Giá phải lớn hơn 0"]
  }
}
```
