# 📋 Upcoming Tasks

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-07 (session 6 — all 4 original feature groups complete)
> Status: **Production live** at https://web-develop-production.up.railway.app

---

## ✅ Done — Session 4 (2026-06-06)

| Task | File(s) |
|------|---------|
| **An Phát scraper** — JSON API, 8 categories, socket/RAM/storage fixes | `scraper/scrapers/anphat.py`, `scraper/main.py` |
| **GitHub Actions cron** — split 4 parallel matrix jobs, Node.js 24 fix | `.github/workflows/scraper.yml` |
| **Gán Admin role** cho `duylamasd1995@gmail.com` | Supabase SQL |
| **appsettings.Production.json** | `web/appsettings.Production.json` |
| **Dockerfile** (multi-stage build, layer caching) | `Dockerfile` |
| **Deploy Railway**: PORT bind, ForwardedHeaders proxy trust, DataProtection DB persistence | `web/Program.cs`, `web/TechSpecs.csproj`, `web/Data/AppDbContext.cs` |
| EF migration `AddDataProtectionKeys` — bảng lưu DataProtection keys qua redeploy | `web/Data/Migrations/` |
| Fix OAuth state cookie: `SameSite=None; Secure` để Chrome không reject | `web/Program.cs` |
| Fix rolling-deploy key mismatch: bỏ `SetApplicationName`, clear old keys | `web/Program.cs`, Supabase |
| **xUnit**: 44/44 pass — Cart, Order, CompatibilityEngine | `TechSpecs.Tests/` |
| **Playwright E2E**: 23/23 pass — auth, products, cart, builder | `tests/e2e/` |

---

## ✅ Done — Session 3 (2026-06-06)

| Task | File(s) |
|------|---------|
| **Save Build**: model `SavedBuild`, migration `AddSavedBuilds`, nút Save + modal trong Builder | `Models/SavedBuild.cs`, `Controllers/BuildController.cs`, `Views/Builder/Index.cshtml` |
| **Share Build**: `/Build/Share/{token}` — public page, "Build This PC" preload | `Views/Build/Share.cshtml` |
| **My Builds page**: list builds, copy link, open in Builder, delete | `Views/Build/MyBuilds.cshtml` |
| "My Builds" link trong navbar dropdown | `Views/Shared/_Layout.cshtml` |
| **Advanced Filters** Products page: price range (min/max) + brand checkboxes per category | `Views/Products/Index.cshtml` |
| `GET /Products/Brands?category=xxx` — endpoint trả về brands cho filter | `Controllers/ProductsController.cs` |
| Fix `auth.spec.ts` — check FullName thay vì email prefix sau khi đổi navbar | `tests/e2e/auth.spec.ts` |

---

## ✅ Done — Session 2 (2026-06-04)

| Task | File(s) |
|------|---------|
| Admin role gán cho 2 accounts qua Supabase SQL | — |
| Motherboard socket fix: `_mb_socket_from_name()` map 20+ chipsets | `scraper/scrapers/phongvu.py` |
| Product Detail page `GET /Products/Detail/{category}/{id}` | `ProductsController.cs`, `Views/Products/Detail.cshtml` |
| Products cards có link click → detail | `_ProductGrid.cshtml`, `Index.cshtml` |
| Profile page `GET/POST /Account/Profile` — edit name, change password | `AccountController.cs`, `Views/Account/Profile.cshtml` |
| Navbar hiển thị FullName (custom claim) thay vì email prefix | `Services/AppUserClaimsPrincipalFactory.cs`, `Program.cs` |
| VRAM fix: `_vram_from_name()` regex sanity-check 1–48GB | `scraper/scrapers/phongvu.py` |
| CPU TDP lookup table: `_tdp_from_name()` 50+ models, xử lý `™®` | `scraper/scrapers/phongvu.py` |
| GPU tier map thêm RTX 5000 series + AMD RX 9000 | `scraper/scoring/performance.py` |
| TTGShop scraper — static HTML, 8 categories | `scraper/scrapers/ttgshop.py` |
| GearVN scraper — Haravan, sitemap-based, JSON price ×100 | `scraper/scrapers/gearvn.py` |
| `main.py` hỗ trợ `--source phongvu,ttgshop,gearvn` flag | `scraper/main.py` |

---

## ✅ Testing Infrastructure (Done)

- [x] **xUnit** (`TechSpecs.Tests/`) — **44/44 pass**
  - `CompatibilityEngineTests` — 19 tests: socket, RAM, GPU length, PSU wattage, cooler TDP, scoring
  - `CartTests` — 12 tests: add/remove/update/clear/count/isolation
  - `OrderTests` — 13 tests: checkout flow, total calc, cart clear, user isolation, detail
  - Run: `dotnet test TechSpecs.Tests/`
- [x] **Playwright E2E** (`tests/e2e/`) — **23/23 pass** ✅ restored 2026-06-07
  - `auth.spec.ts` — register, login error, logout, protected routes
  - `products.spec.ts` — grid render, add to cart toast/badge, category filter
  - `cart.spec.ts` — empty state, add items, remove, full checkout flow, validation
  - `builder.spec.ts` — load, select component, price update, add all to cart
  - Run: `cd tests && npm test` (requires app running on port 5003)

---

## Priority 2 — Admin Dashboard ✅ Done

- [x] `[Authorize(Roles = "Admin")]` cho toàn bộ Admin area
- [x] **Admin/Dashboard**: tổng quan (số user, đơn hàng, doanh thu, sản phẩm theo danh mục)
- [x] **Admin/Products**: CRUD cho 8 loại linh kiện — list theo category, edit form đầy đủ fields, xoá
- [x] **Admin/Orders**: quản lý đơn hàng, filter theo status, update status qua dropdown AJAX
- [x] **Admin/Scraper**: nút kích hoạt Python scraper (chạy nền với Process.Start)
- [x] **Gán Admin role**: đã gán cho `quockhanhasd1@gmail.com` + `caokhanhasd@gmail.com` qua Supabase SQL. `duylamasd1995@gmail.com` chờ đăng ký app → báo để gán tiếp

---

## Priority 3 — UX & Polish

### Builder Improvements ✅ All done
- [x] **Lưu build**: nút "Save Build" cho user đã đăng nhập (bảng `SavedBuild`)
- [x] **Share build**: generate link dạng `/Build/Share/{token}`
- [x] Highlight compatibility warnings trực quan
- [x] Hiển thị `RecommendedPsuWattage` rõ ràng khi chọn CPU/GPU
- [x] Tính năng "Thế hệ linh kiện" (Generation Badges) và "Auto-Pairing"
- [x] Fix lưu phiên chat AI (`sessionStorage`) và nút Open in Builder

### Products Page ✅ All done
- [x] **Product Detail page** — ảnh lớn, full specs, Add to Cart + Add to Builder
- [x] Thêm filter nâng cao: khoảng giá (min-max), brand checkbox per category
- [x] Pagination nút "Next / Prev"

### Auth
- [x] Trang **Profile** — đổi FullName, xem email, đổi password
- [x] **Forgot password** flow — Resend API, 4 pages (ForgotPassword → Confirmation → ResetPassword → ResetPasswordConfirmation), link trong Login page

---

## Priority 4 — Scraper & Data Quality

- [x] Cải thiện socket extraction cho Motherboard
- [x] Cải thiện TDP cho CPU
- [x] Re-scrape + fix data CPU + GPU
- [x] Thêm scraper cho **TTGShop**
- [x] Thêm scraper cho **GearVN**
- [x] **Chạy scraper định kỳ**: GitHub Actions cron job mỗi 12 giờ, 4 parallel jobs (phongvu/ttgshop/gearvn/anphat)
- [x] Thêm scraper cho **An Phát** (`anphatpc.com.vn`) — JSON API, 8 categories, ~1000 products

---

## Priority 5 — Deployment ✅ Done

- [x] **Deploy web lên Railway** — live tại `https://web-develop-production.up.railway.app`
- [x] **`appsettings.Production.json`** — logging warning level
- [x] **Dockerfile** — multi-stage, layer caching
- [x] **Google OAuth** — redirect URI đã thêm vào Google Cloud Console
- [x] DataProtection keys persisted to PostgreSQL (survive redeploy)
- [x] ForwardedHeaders trust Railway proxy (HTTPS scheme, OAuth state valid)
- [ ] Custom domain (tùy chọn — Railway domain hiện tại hoạt động tốt)

---

## Priority 6 — Tính năng nâng cao ✅ All done

- [x] **So sánh build**: checkboxes trong MyBuilds, floating bar "A vs B → So sánh", trang side-by-side 8 rows
- [x] **Build community**: trang `/Community` public builds sorted by upvotes, nút Publish/Upvote
- [x] **Price history**: scraper ghi vào `price_history` mỗi khi giá thay đổi, Chart.js trên Product Detail
- [x] **Wishlist**: heart button trên Product Detail, trang `/Wishlist`, Add to Cart từ wishlist
- [x] **Multi-language**: ASP.NET Core localization, SharedResource.vi.resx, VI/EN toggle trong navbar
- [x] **Dark/Light mode toggle**: sun/moon button navbar, `.light-mode` CSS, persist localStorage

---

---

## ✅ Done — Session 6 (2026-06-07) — All 4 original feature groups complete

| Task | File(s) |
|------|---------|
| **P7 Warranty Check** — tra cứu SĐT/serial, auto-tạo khi Confirmed | `Models/WarrantyRecord.cs`, `Controllers/WarrantyController.cs`, `Views/Warranty/` |
| **P8 One-page Checkout** — COD/bank transfer, coupon TECHSPECS10=10%, AJAX | `Views/Orders/Checkout.cshtml`, `Models/Order.cs` (PaymentMethod+DiscountAmount) |
| **Stock Status 5 trạng thái** — Còn hàng/Còn ít/Hết hàng/Sắp về/Liên hệ | `Models/*.cs` (StockStatusOverride), `ViewModels/ProductListItem.cs`, `_ProductGrid.cshtml` |
| **Public Order Tracking** — /OrderTracking không cần login | `Controllers/OrderTrackingController.cs`, `Views/OrderTracking/` |
| **Admin Warranty** — bảng quản lý bảo hành, search, ngày hết hạn | `Controllers/AdminController.cs`, `Views/Admin/Warranties.cshtml` |
| **Reviews & Q&A** — star rating, comment, hỏi đáp thread | `Models/ProductReview.cs`, `Controllers/ReviewsController.cs`, `Views/Products/Detail.cshtml` |
| **Cross-selling** — gợi ý linh kiện phù hợp trên Product Detail | `Controllers/ProductsController.cs` (CrossSell endpoint) |

---

## ✅ Done — Session 7 (2026-06-07) — P9–P17 complete

| Task | File(s) |
|------|---------|
| **P9 — Export Quotation PDF** | `Controllers/BuilderController.cs`, `Views/Builder/Index.cshtml`, QuestPDF |
| **P10 — Installment Calculator** | `Views/Shared/_Layout.cshtml` (modal + `showInstallment()`), Product Detail + Checkout button |
| **P11 — Flash Sale** | `Models/FlashSale.cs`, `Controllers/AdminController.cs`, `Views/Admin/FlashSales.cshtml`, `Views/Admin/CreateFlashSale.cshtml`, `Controllers/ProductsController.cs` (`ActiveSales`), `Views/Products/Index.cshtml` (countdown overlay) |
| **P12 — Coupon DB** | `Models/Coupon.cs`, `Controllers/AdminController.cs`, `Views/Admin/Coupons.cshtml`, `Views/Admin/CreateCoupon.cshtml`, `Controllers/OrdersController.cs` (`ApplyCouponAsync`) |
| **P15 — My Warranties** | `Controllers/AccountController.cs`, `Views/Account/Warranties.cshtml`, navbar link |
| **P16 — YouTube embed** | `VideoUrl` field on all 8 models + `ProductDetailViewModel`; 16:9 embed on `Detail.cshtml` |
| **P17 — Live Chat** | Floating Zalo + Messenger buttons in `_Layout.cshtml` |

---

## Priority 7 — Remaining (Session 8)

### Group B — Tài chính & Thanh toán
- [ ] **P18 — Payment Gateway (Mock)**: UI + flow đầy đủ cho VNPAY-QR, MoMo, ZaloPay với mock responses.

### Group C — Khuyến mãi
- [ ] **P13 — Combo/Bundle discount**: Model `Bundle` (danh sách sản phẩm + mức giảm giá), auto-apply discount khi cart đủ điều kiện bundle.

### Group D — CRM & Loyalty
- [ ] **P14 — Membership Tiers**: `LoyaltyPoints` table, 4 bậc (Đồng <1tr / Bạc <5tr / Vàng <20tr / Kim Cương 20tr+), tích điểm khi mua hàng, hiển thị tier trên Profile.

### Group E — Nội dung
- [ ] **P19 — Photo upload Reviews**: Supabase Storage bucket, upload ảnh review, hiển thị thumbnail dưới comment.

---

## Thứ tự làm (session 8)
P14 → P13 → P18 → P19

---

## Bugs & Known Issues

| Issue | Mức độ | Ghi chú |
|-------|--------|---------|
| ~~Motherboard `SocketCompatibility` = "Unknown" cho 100% sản phẩm~~ | ~~Medium~~ | Đã fix ✓ |
| ~~VRAM hiển thị sai (VD: "5060 GB")~~ | ~~Medium~~ | Đã fix ✓ |
| ~~TDP = 0W cho nhiều CPU~~ | ~~Low~~ | Đã fix ✓ |
| ~~CPU performance score = 0 cho ~27% sản phẩm~~ | ~~Low~~ | Đã fix: lookup table 60+ CPU models + range GHz pattern ✓ |
| `dotnet run` báo port 5003 đã dùng | Low | Dùng `fuser -k 5003/tcp` trước khi chạy |
| ~~`PORT` env var override khiến local dev chạy trên 8080~~ | ~~Low~~ | Đã fix: chỉ bind khi `PORT` env var thực sự được set ✓ |
| ~~Nút "Add to Cart" trên Products/Builder chưa hoạt động~~ | ~~High~~ | Đã xong ✓ |

---
