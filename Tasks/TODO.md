# 📋 Upcoming Tasks

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-06 (session 3 complete)
> Next: Forgot Password → GitHub Actions cron → Deploy Railway

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
- [x] **Playwright E2E** (`tests/e2e/`) — **23/23 pass**
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
- [ ] **Chạy scraper định kỳ**: thiết lập GitHub Actions cron job mỗi 12 giờ
  - File `.github/workflows/scraper.yml`
  - Cần thêm `DATABASE_URL` vào GitHub Secrets
- [ ] Thêm scraper cho **An Phát** (`anphatpc.com.vn`) — tăng độ phủ sản phẩm

---

## Priority 5 — Deployment

- [ ] **Deploy web lên Railway**:
  - Tạo Railway project
  - Thêm environment variables (connection string, API keys)
  - Setup domain `techspecs.up.railway.app`
- [ ] **`appsettings.Production.json`** — cấu hình logging, HSTS
- [ ] **Dockerfile** cho web app
- [ ] Chạy `dotnet-ef database update` trên production DB trước khi deploy

---

## Priority 6 — Tính năng nâng cao (Nice-to-have)

- [ ] **So sánh build**: chọn 2 build để so sánh side-by-side
- [ ] **Build community**: user chia sẻ build, upvote, bình luận
- [ ] **Price history**: lưu lịch sử giá mỗi lần scrape, vẽ biểu đồ
- [ ] **Wishlist**: user lưu sản phẩm yêu thích
- [ ] **Multi-language**: thêm tiếng Việt (hiện tại UI đang tiếng Anh)
- [ ] **Dark/Light mode toggle**

---

## Bugs & Known Issues

| Issue | Mức độ | Ghi chú |
|-------|--------|---------|
| ~~Motherboard `SocketCompatibility` = "Unknown" cho 100% sản phẩm~~ | ~~Medium~~ | Đã fix ✓ |
| ~~VRAM hiển thị sai (VD: "5060 GB")~~ | ~~Medium~~ | Đã fix ✓ |
| ~~TDP = 0W cho nhiều CPU~~ | ~~Low~~ | Đã fix ✓ |
| CPU performance score = 0 cho ~27% sản phẩm (không extract được clock) | Low | Vẫn hiện trong filter, xếp cuối |
| `dotnet run` báo port 5003 đã dùng | Low | Dùng `fuser -k 5003/tcp` trước khi chạy |
| ~~Nút "Add to Cart" trên Products/Builder chưa hoạt động~~ | ~~High~~ | Đã xong ✓ |

---
