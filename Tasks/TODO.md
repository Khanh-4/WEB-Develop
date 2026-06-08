# 📋 Upcoming Tasks

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-08 (session 12 — Homepage redesign complete; P36–P43 queued below)
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

## ✅ Done — Session 8 (2026-06-07) — P13–P19 complete, ALL Priority 7 done

| Task | File(s) |
|------|---------|
| **P14 — Membership Tiers** | `Models/ApplicationUser.cs` (TotalSpend+LoyaltyPoints), `MembershipTier` static class, `Views/Account/Profile.cshtml` (tier card + progress), awarded in `AdminController.UpdateOrderStatus` |
| **P13 — Bundle/Combo** | `Models/Bundle.cs`, `Controllers/AdminController.cs`, `Views/Admin/Bundles.cshtml`, `Views/Admin/CreateBundle.cshtml`, `/Admin/BundlesApi`, `Views/Cart/Index.cshtml` (auto-detect + discount row) |
| **P18 — Payment Mock** | `PaymentMethod` enum +VnPay/MoMo/ZaloPay, 3 new pay cards on Checkout, QR notice, SVG mock QR on Confirmation |
| **P19 — Photo Reviews** | `ImageUrl` on ProductReview, `POST /Reviews/UploadPhoto` (Supabase Storage), file picker + preview in form, thumbnail in review list |

---

## 🎉 All Priority 7 features complete!

**To enable photo reviews:** add to `appsettings.Development.json`:
```json
"Supabase": { "Url": "https://xxx.supabase.co", "AnonKey": "eyJ..." }
```
And create a Storage bucket named `reviews` in Supabase dashboard with public access.

---

## ✅ Done — Session 9 (2026-06-08) — P20–P27 Micro-interactions & Contextual UX

| Task | File(s) |
|------|---------|
| **P20 — Add-to-cart fly animation** | `wwwroot/js/site.js` (`flyToCart()`), `Views/Products/Index.cshtml` |
| **P21 — Skeleton loading** | `wwwroot/css/site.css` (`.sk-box`, `@keyframes skShimmer`), `Views/Products/Index.cshtml` |
| **P22 — Card hover spec overlay** | `wwwroot/css/site.css` (`.card-specs-overlay`), `Views/Products/Index.cshtml` |
| **P23 — Lazy image loading** | `loading="lazy"` on all `<img>` in `renderProducts()` |
| **P24 — Active Filters chips** | `#activeFilters` div + `renderActiveFilters()` in `Views/Products/Index.cshtml` |
| **P25 — Spec tooltips** | `specTips` dict + Bootstrap tooltip init in `Views/Products/Detail.cshtml` |
| **P26 — Sticky Add-to-Cart bar** | `#stickyCartBar` + `IntersectionObserver` in `Views/Products/Detail.cshtml` |
| **P27 — Compare sticky bar** | `.card-cmp-check` checkbox, `#cmpBar`, `toggleCompare()` in `Views/Products/Index.cshtml` |

---

## ✅ Done — Session 10 (2026-06-08) — P28–P35 Feature Polish

| Task | File(s) |
|------|---------|
| **P28 — Killer Specs badges** | `killerSpecKeys[]` + icon chips on `Views/Products/Detail.cshtml` |
| **P29 — Compare diff highlight** | `@section Scripts` in `Views/Products/Compare.cshtml` — green=winner, red=loser, trophy icon |
| **P30 — Quick View modal** | `GET /Products/QuickView/{cat}/{id}`, `.qv-eye-btn` hover, AJAX modal in `Views/Products/Index.cshtml` |
| **P31 — Keyboard shortcuts** | `/` focus search, `Esc` close modal, `←→` navigate Quick View |
| **P32 — FPS Estimator (GPU)** | `ApproximatePerformance` on ViewModel, 7-tier × 7-game card grid in `Views/Products/Detail.cshtml` |
| **P33 — Builder localStorage** | `saveToLocalStorage()`, `clearSavedBuild()`, restore on DOMContentLoaded |
| **P34 — PSU Power Breakdown** | `CpuTDP`/`GpuTDP` on `FilteredResult`, expandable panel in Builder left sidebar |
| **P35 — IMemoryCache** | `AddMemoryCache()`, 60s cache for product lists, 10min for FilterOptions/Brands |

---

## ✅ Done — Session 11 (2026-06-08) — Build Comparison + Admin Benchmarks

| Task | File(s) |
|------|---------|
| **Build Compare** — Snapshot Build A button (≥3 components), slide-in panel, Chart.js 5-axis radar | `Views/Builder/Index.cshtml`, `Controllers/BuilderController.cs` |
| **POST /Builder/CompareBuilds** — hybrid benchmark (DB real data + ApproxPerf fallback), radar scores, 4 spec groups | `Controllers/BuilderController.cs`, `ViewModels/CompareViewModels.cs` |
| **ComponentBenchmarks table** — EF migration, Admin CRUD (`/Admin/Benchmarks`) | `Models/ComponentBenchmark.cs`, `Controllers/AdminController.cs`, `Views/Admin/Benchmarks.cshtml` |
| **Allow multiple reviews** — drop unique index, always insert new review | `Controllers/ReviewsController.cs`, migration `AllowMultipleReviews` |
| **Bug fix: P27 compare URL** — was `/Build/Compare?a=cat_id` (broken), fixed to `/Products/Compare?category=&ids=` | `Views/Products/Index.cshtml` |

---

## ✅ Done — Session 12 (2026-06-08) — Homepage Redesign

| Task | File(s) |
|------|---------|
| **2-row sticky header** — brand bar (logo + 45%-wide always-visible search + hotline 1900 6969 + mini-cart hover panel) + nav row | `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css` |
| **Asymmetrical hero** — col-lg-3 vertical menu + col-lg-9 carousel (420px height), removed right-side banners | `Views/Home/Index.cshtml` |
| **5-column CSS Grid** — `.product-grid-5` responsive (5→4→3→2), replaces horizontal-scroll | `wwwroot/css/site.css` |
| **Card micro-interactions** — purple border + shadow on hover, `.card-action-overlay` slides up (Add-to-cart + Compare) | `wwwroot/css/site.css` |
| **3-zone badge system** — `.badge-status` top-left, `.badge-discount` top-right, `.badge-gift` bottom strip | `wwwroot/css/site.css` |
| **Mini-cart hover dropdown** — `/Cart/MiniCart` partial, cached until cart changes via `invalidateMiniCart()` | `Controllers/CartController.cs`, `Views/Cart/_MiniCart.cshtml`, `wwwroot/js/site.js` |
| **FeaturedCategoryViewComponent** — self-contained DB query, renders 10 products per section (CPU/GPU/RAM/SSD/MB) | `ViewComponents/FeaturedCategoryViewComponent.cs`, `Views/Shared/Components/FeaturedCategory/` |
| **FlashSaleViewComponent** — self-contained, live countdown timer | `ViewComponents/FlashSaleViewComponent.cs`, `Views/Shared/Components/FlashSale/` |
| **HomeController simplified** — removed 8-category query loop, offloaded to View Components | `Controllers/HomeController.cs`, `ViewModels/HomeViewModel.cs` |

---

## ✅ Done — Session 13 (2026-06-08) — P36–P43 Social Proof, Mobile UX, Performance

> Mục tiêu: Kích hoạt "tâm lý đám đông" cho dân công nghệ — vốn rất lý trí nhưng nhạy cảm với cộng đồng và đánh giá.

## Priority 8 — Social Proof & FOMO (P36–P38) ✅ Done

### P36 — Mini-Toast Purchase Notification

**Target:** `Views/Shared/_Layout.cshtml` + `wwwroot/js/site.js`

**Mô tả:** Cứ 30–45 giây, hiện pop-up nhỏ ở góc dưới-trái: *"Anh K. tại TP.HCM vừa đặt mua RTX 4070 Ti"* — tạo cảm giác cửa hàng đang tấp nập.

**Implementation:**
- Dữ liệu: array mock (~10 entries) hoặc endpoint `GET /Orders/RecentActivity` trả về 10 đơn hàng thật gần nhất (lấy từ `Orders` table, ẩn họ tên = chỉ giữ tên viết tắt + thành phố)
- JS: `setInterval` 30–45s (random để không bị robot), gọi `showPurchaseToast(item)`
- CSS: `.purchase-toast` — slide-in từ dưới-trái, tự dismiss sau 5s, z-index thấp hơn modal
- Format: avatar icon (bi-person-circle) + text + ảnh sản phẩm nhỏ (thumbnail 40×40)
- Delay đầu tiên: 15s sau page load (không bắn ngay gây khó chịu)

**Files:** `_Layout.cshtml`, `wwwroot/js/site.js`, `wwwroot/css/site.css`, (optional) `Controllers/OrdersController.cs`

---

### P37 — Star Ratings trên Product Card

**Target:** `Views/Shared/Components/FeaturedCategory/Default.cshtml` + `Views/Products/Index.cshtml` (`_ProductGrid` partial nếu có)

**Mô tả:** Mỗi product card hiển thị ⭐⭐⭐⭐½ (4.5 · 128) ngay dưới tên sản phẩm.

**Implementation:**
- Backend: `GET /Products/RatingSummary?category=cpu&ids=1,2,3,5` — query `ProductReviews` table, GROUP BY `ProductId` → avg rating + count
- Hoặc đơn giản hơn: thêm `AvgRating` + `ReviewCount` vào `ProductListItem` và populate trong `FeaturedCategoryViewComponent` bằng 1 sub-query (LEFT JOIN hoặc correlated sub-select)
- Frontend: render 5 sao bằng CSS clip-path hoặc Bootstrap Icons `bi-star-fill` / `bi-star-half` / `bi-star`
- Hiển thị: chỉ show nếu `ReviewCount > 0`; nếu 0 thì ẩn (không hiện "0 đánh giá")
- Mobile: sao nhỏ hơn (font-size .75rem) để không chiếm quá nhiều chỗ

**Files:** `ViewComponents/FeaturedCategoryViewComponent.cs`, `Views/Shared/Components/FeaturedCategory/Default.cshtml`, `ViewModels/ProductListItem.cs`, `wwwroot/css/site.css`

---

### P38 — "Sản phẩm bạn vừa xem" (Recently Viewed)

**Target:** `Views/Home/Index.cshtml` + `wwwroot/js/site.js` + new API endpoint

**Mô tả:** Khi khách xem sản phẩm rồi back ra trang chủ, thấy ngay khối "Bạn vừa xem" với sản phẩm đó.

**Implementation (100% frontend, không cần đăng nhập):**
- `localStorage` key: `recentlyViewed` — array tối đa 10 items: `[{id, category, name, price, imageUrl, viewedAt}]`
- Ghi vào localStorage: trên trang Product Detail (`/Products/Detail/{cat}/{id}`), thêm JS ghi item vào đầu array, dedup, cắt ≤10
- Hiển thị trên homepage: `DOMContentLoaded` → đọc localStorage → nếu có items → `fetch('/Products/RecentlyViewed', {method:'POST', body: JSON.stringify(ids)})` → API trả về fresh data (price/stock mới nhất) → render section
- API `POST /Products/RecentlyViewed`: nhận `{items: [{id, category}]}`, query DB, trả về `List<ProductListItem>` (max 10)
- Vị trí: ngay trước Flash Sale section trên Index.cshtml, ẩn khi không có data
- CSS: dùng lại `.product-grid-5` + `.component-card` có sẵn

**Files:** `Views/Products/Detail.cshtml` (ghi localStorage), `Views/Home/Index.cshtml` (section + JS), `Controllers/ProductsController.cs` (endpoint mới), `wwwroot/js/site.js`

---

## Priority 9 — Mobile App-like UX (P39–P41) ✅ Done

> Mục tiêu: Mobile chiếm >50% traffic nhưng UX hiện tại vẫn "thu nhỏ desktop". Cần trải nghiệm như Shopee/Tiki.

### P39 — Bottom Navigation Bar (Mobile Only)

**Target:** `Views/Shared/_Layout.cshtml` + `wwwroot/css/site.css` + `wwwroot/js/site.js`

**Mô tả:** 4 nút ở mép dưới màn hình (Home / Danh mục / Giỏ hàng / Tài khoản) — thao tác hoàn toàn bằng ngón cái.

**Implementation:**
```html
<nav class="bottom-nav d-lg-none" id="bottomNav">
  <a href="/"       class="bottom-nav-item active"><i class="bi bi-house-fill"></i><span>Trang chủ</span></a>
  <a href="/Products" class="bottom-nav-item"><i class="bi bi-grid-fill"></i><span>Danh mục</span></a>
  <a href="/cart"   class="bottom-nav-item position-relative"><i class="bi bi-cart3"></i><span>Giỏ hàng</span><span class="bottom-cart-badge" id="bottomCartBadge"></span></a>
  <a href="/Account/Profile" class="bottom-nav-item"><i class="bi bi-person-fill"></i><span>Tài khoản</span></a>
</nav>
```
- CSS: `position: fixed; bottom: 0; left: 0; right: 0; height: 60px; z-index: 1100; background: rgba(10,6,22,.97); border-top: 1px solid rgba(255,255,255,.1); display: flex;`
- Thêm `padding-bottom: 60px` vào `<main>` trên mobile để tránh bị che
- Header auto-hide khi scroll down: `IntersectionObserver` hoặc `scroll` event với debounce — `transform: translateY(-100%)` khi scroll xuống, hiện lại khi scroll lên
- Cart badge: đồng bộ với `updateCartBadge()` hiện có — thêm `#bottomCartBadge` vào function đó
- Active state: highlight icon tương ứng trang hiện tại (check `window.location.pathname`)

**Files:** `Views/Shared/_Layout.cshtml`, `wwwroot/css/site.css`, `wwwroot/js/site.js`

---

### P40 — Horizontal Swipe cho Product Blocks (Mobile Touch)

**Target:** `wwwroot/css/site.css` + `Views/Home/Index.cshtml`

**Mô tả:** Trên mobile (<768px), khối sản phẩm chuyển từ grid 2 cột → carousel vuốt ngang như Shopee.

**Implementation:**
```css
@media (max-width: 767px) {
  .product-grid-5 {
    display: flex;
    overflow-x: auto;
    scroll-snap-type: x mandatory;
    -webkit-overflow-scrolling: touch;
    gap: .75rem;
    padding-bottom: 8px;
  }
  .product-grid-5::-webkit-scrollbar { display: none; }
  .product-grid-5 .component-card {
    flex: 0 0 78vw;   /* mỗi card chiếm 78% viewport width */
    max-width: 300px;
    scroll-snap-align: center;
  }
}
```
- Thêm mũi tên chỉ dẫn (→) ở góc phải section title trên mobile để hint "có thể vuốt"
- Flash Sale section dùng chung class `product-grid-5` → tự động được swipeable

**Files:** `wwwroot/css/site.css`

---

### P41 — Full-screen Search Overlay (Mobile)

**Target:** `Views/Shared/_Layout.cshtml` + `wwwroot/js/site.js` + `wwwroot/css/site.css`

**Mô tả:** Khi tap vào search bar trên mobile, mở full-screen overlay (như iOS/Android native search).

**Implementation:**
```css
.search-overlay {
  position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
  z-index: 9998; background: rgba(10,6,22,.98);
  display: flex; flex-direction: column;
  transform: translateY(-100%);
  transition: transform .25s ease;
}
.search-overlay.open { transform: translateY(0); }
```
- Structure: header (input + nút Huỷ) + body (lịch sử tìm kiếm từ localStorage + danh mục nhanh)
- Trigger: `@media (max-width: 767px)` — khi focus `#navSearchInput`, `openSearchOverlay()`; nếu là desktop thì không làm gì (live search bình thường)
- Lịch sử: lưu vào `localStorage['searchHistory']` (array 10 items), hiển thị với nút xoá từng item
- Trending: 5 từ khoá hardcode (VD: "RTX 4070", "Intel i5-14400F", "RAM DDR5 32GB", "SSD NVMe 1TB", "Mainboard B760")
- Live search kết quả: dùng lại endpoint `/Products/LiveSearch` hiện có

**Files:** `Views/Shared/_Layout.cshtml`, `wwwroot/js/site.js`, `wwwroot/css/site.css`

---

## Priority 10 — Performance & SEO (P42–P43) ✅ Done

> Mục tiêu: Homepage load trong chớp mắt, không giật layout, Google không phạt.

### P42 — ASP.NET Core Output Caching

**Target:** `Program.cs` + `ViewComponents/FeaturedCategoryViewComponent.cs` + `ViewComponents/FlashSaleViewComponent.cs`

**Yêu cầu:** Dùng Output Caching middleware (.NET 7+), KHÔNG dùng `ResponseCaching` cũ.

**Step 1 — Service Registration (`Program.cs`):**
```csharp
builder.Services.AddOutputCache(options => {
    options.AddPolicy("FeaturedProducts", b => b.Expire(TimeSpan.FromMinutes(10)).Tag("home-products"));
    options.AddPolicy("FlashSale",        b => b.Expire(TimeSpan.FromSeconds(60)).Tag("flash-sale"));
});
// Thứ tự quan trọng:
app.UseRouting();
app.UseOutputCache();   // sau UseRouting, trước UseAuthorization
app.UseAuthorization();
```

**Step 2 — View Component Caching:**
Vì `[OutputCache]` attribute không áp dụng trực tiếp lên View Components, wrap bằng middleware route hoặc dùng `IOutputCacheStore` thủ công trong `InvokeAsync`:
```csharp
// Option A: cache toàn bộ response của endpoint gọi component (nếu có route riêng)
// Option B: inject IOutputCacheStore + IMemoryCache làm layer thứ 2 trong component
// Option C (recommended simple): dùng IMemoryCache có sẵn (đã cài P35), upgrade duration
```
- `FeaturedCategoryViewComponent`: cache 10 phút per category key (`home-cat-{category}`)
- `FlashSaleViewComponent`: cache 60 giây (vì có countdown + sold quantity thay đổi)

**Step 3 — Cache Eviction (`AdminController.cs`):**
```csharp
// Inject IOutputCacheStore _cacheStore
// Khi admin update giá hoặc thêm flash sale:
await _cacheStore.EvictByTagAsync("home-products", cancellationToken);
await _cacheStore.EvictByTagAsync("flash-sale", cancellationToken);
```
- Gọi eviction sau `SaveChangesAsync()` trong các action: `EditProduct`, `CreateFlashSale`, `DeleteFlashSale`

**Files:** `Program.cs`, `ViewComponents/FeaturedCategoryViewComponent.cs`, `ViewComponents/FlashSaleViewComponent.cs`, `Controllers/AdminController.cs`

---

### P43 — Zero CLS (Cumulative Layout Shift) Fixes

**Target:** `wwwroot/css/site.css` + tất cả view có `<img loading="lazy">`

**Mô tả:** Ngăn trang bị "giật" khi ảnh lazy-load xong — Google phạt CLS rất nặng trong SEO ranking.

**Implementation:**

1. **Image wrapper aspect-ratio** — thay vì để chiều cao cố định bằng `style="height:130px"`, dùng CSS container:
```css
.card-img-box {
  aspect-ratio: 1 / 1;      /* vuông */
  overflow: hidden;
  display: flex; align-items: center; justify-content: center;
  background: rgba(255,255,255,.02);
}
.card-img-box img {
  width: 100%; height: 100%;
  object-fit: contain;
}
```

2. **Skeleton có kích thước cố định** — `.sk-box` cần có `height` và `aspect-ratio` explicit, không để browser tự tính:
```css
.skeleton-card { aspect-ratio: 3/4; min-height: 240px; }
```

3. **Font loading** — thêm `font-display: swap` nếu dùng Google Fonts (hiện tại dùng Bootstrap Icons CDN — cần thêm `&display=swap` hoặc self-host)

4. **Hero carousel** — `<img>` đầu tiên (slide 0) không nên lazy-load; thêm `loading="eager"` + `fetchpriority="high"` để LCP nhanh:
```html
<img src="..." loading="eager" fetchpriority="high" ...>   <!-- slide 0 -->
<img src="..." loading="lazy" ...>                         <!-- slide 1, 2 -->
```

5. **Product cards** — thay `style="height:130px;object-fit:contain"` thành class `.card-img-box` ở tất cả places trong `FeaturedCategory/Default.cshtml`, `FlashSale/Default.cshtml`, `Index.cshtml`

**Files:** `wwwroot/css/site.css`, `Views/Shared/Components/FeaturedCategory/Default.cshtml`, `Views/Shared/Components/FlashSale/Default.cshtml`, `Views/Home/Index.cshtml`

---

## Thứ tự thực hiện gợi ý

| Thứ tự | Task | Lý do ưu tiên |
|--------|------|--------------|
| 1 | **P43 Zero CLS** | Nhanh, ảnh hưởng SEO ngay, không có dependency |
| 2 | **P42 Output Cache** | Giảm tải DB ngay, ảnh hưởng toàn site |
| 3 | **P39 Bottom Nav** | Impact lớn với mobile traffic |
| 4 | **P40 Horizontal Swipe** | Chỉ là CSS override, nhanh |
| 5 | **P37 Star Ratings** | Cần DB query nhưng UI đơn giản |
| 6 | **P38 Recently Viewed** | localStorage + 1 API endpoint |
| 7 | **P36 Purchase Toast** | Mock data đơn giản, impact tâm lý cao |
| 8 | **P41 Search Overlay** | Mobile UX, phức tạp nhất nhóm này |

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
