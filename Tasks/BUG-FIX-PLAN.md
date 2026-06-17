# Bug Fix Plan — TechSpecs QA Report
**Ngày tạo:** 2026-06-16  
**Nguồn:** Check lỗi.docx (15 bugs từ QA review)  
**Trạng thái:** Phase 1 XONG (2026-06-17) — Phase 2 & 3 còn lại

---

## Tổng quan

| Phase | Bugs | Ước tính |
|-------|------|---------|
| Phase 1 — CRITICAL | BUG-01, BUG-07, BUG-10 | ~2–3h |
| Phase 2 — HIGH | BUG-04, BUG-03, BUG-13, BUG-14, BUG-08, BUG-15 | ~4–5h |
| Phase 3 — MEDIUM | BUG-11, BUG-09, BUG-05, BUG-12, BUG-02 | ~2–3h |
| Skip | BUG-06 | — |

---

## Phase 1 — CRITICAL ✅ XONG (2026-06-17)

> **Kết quả:**
> - **BUG-01** — FIXED: `ForgotPassword` POST giờ bọc try/catch quanh `SendEmailAsync`; lỗi gửi mail (Resend misconfig/domain/network) được log qua `ILogger<AccountController>` thay vì ném 500; user luôn thấy trang Confirmation trung lập.
> - **BUG-07** — ĐÃ CÓ SẴN (làm ở session trước): `Profile.cshtml` đã có form đổi mật khẩu (ẩn với Google account), backend `Profile` POST dùng `ChangePasswordAsync`, hiển thị `TempData["Success"]`. Không cần sửa.
> - **BUG-10** — FIXED: form `QuickQuote` + backend đã đúng từ trước nhưng `Home/Index.cshtml` không render flash message → user tưởng nút không hoạt động. Thêm bridge `TempData["SuccessMessage"]`/`["ErrorMessage"]` → `showToast()` trong `_Layout.cshtml` (toàn site).
> - Build: 0 errors, 0 warnings.

### BUG-01: Password reset → 500 error
**Mô tả:** Nhấn "Gửi link đặt lại mật khẩu" qua Gmail → trang 500 "An error occurred"  
**Root cause:** `ForgotPassword` action gọi `_emailSender.SendEmailAsync()` nhưng Resend config sai hoặc chưa handle exception  
**Files cần sửa:**
- `Controllers/AccountController.cs` — action `ForgotPassword` POST
- `Services/EmailSender.cs` (nếu có) hoặc email sender registration

**Subtasks:**
- [ ] BUG-01a: Kiểm tra `AccountController.ForgotPassword` — xem exception bị throw ở đâu
- [ ] BUG-01b: Kiểm tra Resend API key trong `appsettings.Development.json`
- [ ] BUG-01c: Add try/catch với friendly error message thay vì 500
- [ ] BUG-01d: Test flow: nhập email → nhận link → click link → đặt lại mật khẩu

---

### BUG-07: Không đổi được mật khẩu trong hồ sơ cá nhân
**Mô tả:** Trang Profile không có form đổi mật khẩu  
**Root cause:** Feature chưa được implement  
**Files cần sửa:**
- `Controllers/AccountController.cs` — thêm `ChangePassword` GET/POST
- `ViewModels/ProfileViewModel.cs` — thêm `ChangePasswordViewModel`
- `Views/Account/Profile.cshtml` — thêm section đổi mật khẩu
- Lưu ý: Google OAuth users không có password → ẩn form

**Subtasks:**
- [ ] BUG-07a: Thêm `ChangePasswordViewModel` (CurrentPassword, NewPassword, ConfirmPassword)
- [ ] BUG-07b: Thêm `POST /Account/ChangePassword` action (dùng `_userManager.ChangePasswordAsync`)
- [ ] BUG-07c: Thêm collapsible form trong `Profile.cshtml` — ẩn nếu login via Google
- [ ] BUG-07d: Show success/error toast sau khi submit

---

### BUG-10: Form "Nhận báo giá nhanh" — nút không hoạt động
**Mô tả:** Nút "Gửi yêu cầu" click bị redirect về trang chủ, không có thông báo  
**Root cause:** Form action chưa được wire, hoặc JS submit bị ngăn và redirect về `/`  
**Files cần sửa:**
- `Views/Home/Index.cshtml` — form "Nhận báo giá nhanh"
- `Controllers/HomeController.cs` hoặc `QuoteController` — endpoint nhận form

**Subtasks:**
- [ ] BUG-10a: Tìm form "Nhận báo giá nhanh" trong views (grep `báo giá`)
- [ ] BUG-10b: Kiểm tra form action và method — đang submit đi đâu
- [ ] BUG-10c: Wire vào `POST /Home/QuoteRequest` hoặc existing `QuoteRequestsController`
- [ ] BUG-10d: Lưu vào DB bảng `quote_requests` (đã có) + show toast success
- [ ] BUG-10e: Validate input trước khi submit (SĐT/email bắt buộc)

---

## Phase 2 — HIGH

### BUG-04: Cải thiện hệ thống tra cứu đơn hàng
**Mô tả:**
- Khi bấm "Đơn hàng" trên navbar → kiểm tra đăng nhập
- Chưa đăng nhập → form tra cứu bằng SĐT hoặc mã đơn (tùy chọn)
- Đã đăng nhập → hiển thị danh sách đơn hàng của account, newest first

**Hiện trạng:**
- `OrdersController` — `[Authorize]`, `/Orders/Index` đã list đơn theo account ✓  
- `OrderTrackingController.Check()` — yêu cầu CẢ orderId + phone (cần fix: phone-only)
- `Confirmation.cshtml` — đã hiện thông tin đơn sau thanh toán ✓

**Files cần sửa:**
- `Controllers/OrderTrackingController.cs` — mở rộng `Check()` hỗ trợ phone-only → trả về array
- `Views/OrderTracking/Index.cshtml` — UI mới: phone field + optional mã đơn + list kết quả
- `Views/Shared/_Layout.cshtml` — link "Đơn hàng" redirect đúng theo auth state

**Subtasks:**
- [ ] BUG-04a: Sửa `Check()` — nếu chỉ có phone → tìm tất cả đơn theo phone, sort `CreatedAt DESC`, trả về array
- [ ] BUG-04b: Sửa `Check()` — nếu có cả orderId + phone → tìm đúng 1 đơn (giữ backward compat)
- [ ] BUG-04c: Cập nhật `OrderTracking/Index.cshtml` — form nhận SĐT (bắt buộc) + Mã đơn (tùy chọn)
- [ ] BUG-04d: Hiển thị list order cards nếu nhiều kết quả (mã đơn, ngày đặt, tổng tiền, trạng thái, link chi tiết)
- [ ] BUG-04e: Cập nhật link "Đơn hàng" trong `_Layout.cshtml` — logged in → `/Orders`, guest → `/OrderTracking`

---

### BUG-03: PC Builder hiển thị tiếng Anh + compare panel hiện sai
**Mô tả:**
- Builder UI dùng tiếng Anh ("Your Build", "Processor", "Motherboard"...)
- Panel "So sánh Build" hiện ngay không cần click nút

**Files cần sửa:**
- `Views/Builder/Index.cshtml`

**Subtasks:**
- [ ] BUG-03a: Dịch labels Builder: "Your Build"→"Cấu hình của bạn", "Processor"→"CPU", "Motherboard"→"Bo mạch chủ", "Memory"→"RAM", "Graphics Card"→"Card đồ họa", "Storage"→"Ổ cứng", "Power Supply"→"Nguồn", "Case"→"Vỏ case", "CPU Cooler"→"Tản nhiệt", "Best Value"→"Giá trị nhất", "Search components..."→"Tìm linh kiện..."
- [ ] BUG-03b: Fix compare panel — ẩn by default, chỉ hiện khi click "So sánh cấu hình"
- [ ] BUG-03c: Thêm nút "So sánh cấu hình" nếu chưa có
- [ ] BUG-03d: Test toàn bộ Builder flow sau khi dịch

---

### BUG-13: Brand filter dùng sai data source
**Mô tả:** Filter "Thương hiệu" hiện "Liên" (từ "Liên hệ đặt hàng...") thay vì brand thật  
**Root cause:** `Brands()` endpoint extract brand từ tên sản phẩm (tên sản phẩm của "Liên hệ đặt hàng Transcend..." → "Liên")

**Files cần sửa:**
- `Controllers/ProductsController.cs` — action `Brands()`

**Subtasks:**
- [ ] BUG-13a: Đọc `ProductsController.Brands()` — xem logic build brand list
- [ ] BUG-13b: Kiểm tra model files — các bảng hardware có field `Brand` không
- [ ] BUG-13c: Nếu có `Brand` field → fix dùng đúng field
- [ ] BUG-13d: Nếu không có → dùng known-brands allowlist hoặc extract từ prefix hợp lý hơn
- [ ] BUG-13e: Filter bỏ brand rác: "Liên", "Tay", "Bộ", "Hộp", v.v.

---

### BUG-14: Không xóa được filter chip đã chọn
**Mô tả:** Nút × trên filter chips không hoạt động; "Xóa tất cả" cũng không hoạt động  
**Root cause:** JS event handler của active filter chips bị lỗi hoặc chưa delegate đúng

**Files cần sửa:**
- `Views/Products/Index.cshtml` — JS phần active filter chips

**Subtasks:**
- [ ] BUG-14a: Tìm và đọc JS handler cho filter chip remove
- [ ] BUG-14b: Debug xem event có được fire không
- [ ] BUG-14c: Fix event handler — dùng event delegation nếu chips render dynamically
- [ ] BUG-14d: Test remove từng loại chip: category, spec (VRAM, capacity), "Xóa tất cả"

---

### BUG-08: Sản phẩm mới → review tab kẹt "Đang tải..."
**Mô tả:** Product không có reviews → spinner không bao giờ dừng  
**Root cause:** AJAX load reviews không handle empty response, spinner không bị remove

**Files cần sửa:**
- `Views/Products/Detail.cshtml` — JS load reviews
- `Controllers/ReviewsController.cs` — response khi 0 reviews

**Subtasks:**
- [ ] BUG-08a: Xem AJAX endpoint reviews — trả về gì khi 0 records
- [ ] BUG-08b: Kiểm tra JS handler response rỗng
- [ ] BUG-08c: Fix: response rỗng → ẩn spinner, hiện "Chưa có đánh giá nào. Hãy là người đầu tiên!"
- [ ] BUG-08d: Test với product có reviews và product 0 reviews

---

### BUG-15: Tablet ~1024px → trang trắng hoàn toàn
**Mô tả:** iPad Pro 1024×1366px → chỉ hiện header, còn lại blank  
**Root cause:** CSS breakpoint ẩn content ở ~1024px

**Files cần sửa:**
- `wwwroot/css/site.css` — responsive breakpoints

**Subtasks:**
- [ ] BUG-15a: Mở DevTools 1024px → xem element nào bị `display:none` hoặc overflow hidden
- [ ] BUG-15b: Tìm breakpoint guilty trong `site.css` (grep `1024`, `1025`, `992`)
- [ ] BUG-15c: Fix breakpoint — content visible ở 768px–1200px range
- [ ] BUG-15d: Test các trang: Home, Products, Builder, Cart ở 1024px

---

## Phase 3 — MEDIUM

### BUG-11: Light theme — text bị invisible/khó đọc
**Mô tả:** Light theme → text trùng màu nền trên nhiều trang  
**Files:** `wwwroot/css/site.css`, các `.cshtml` có inline color hardcoded

**Subtasks:**
- [ ] BUG-11a: Audit Home page ở light mode (product cards, stat numbers)
- [ ] BUG-11b: Audit Products page ở light mode (filter panel, grid)
- [ ] BUG-11c: Audit AI Chatbot widget ở light mode
- [ ] BUG-11d: Fix tất cả hardcoded colors → `var(--text)` / `var(--text-muted)` / `var(--surface)`
- [ ] BUG-11e: Test toggle dark↔light trên tất cả pages chính

---

### BUG-09: Broken product images
**Mô tả:** Một số sản phẩm bị broken image trong Recently Viewed và product grid  
**Files:** `Views/Products/Detail.cshtml`, `Views/Products/Index.cshtml`, `Views/Home/Index.cshtml`

**Subtasks:**
- [ ] BUG-09a: Tạo `/wwwroot/images/placeholder.jpg` (hoặc dùng `/images/no-image.svg`)
- [ ] BUG-09b: Thêm `onerror` fallback cho tất cả `<img>` product ở 3 locations
- [ ] BUG-09c: Test với product có ImageUrl null và URL expired

---

### BUG-05: Floating chat buttons kích thước không đồng nhất
**Mô tả:** Zalo/Messenger/TechSpecs AI buttons có size khác nhau  
**Files:** `Views/Shared/_ChatWidget.cshtml`

**Subtasks:**
- [ ] BUG-05a: Xem CSS floating buttons trong `_ChatWidget.cshtml`
- [ ] BUG-05b: Set uniform size: `width:48px; height:48px` cho cả 3 buttons
- [ ] BUG-05c: Đảm bảo icons scale đều nhau

---

### BUG-12: PC Builder/Cộng đồng label overflow trong mega menu dropdown
**Mô tả:** Text overflow ra ngoài khung trong mega menu popup  
**Note:** Navbar buttons đã fixed session 22; đây là labels bên trong dropdown  
**Files:** `Views/Shared/Components/CategoryMenu/Default.cshtml`

**Subtasks:**
- [ ] BUG-12a: Xác định element bị overflow trong mega menu
- [ ] BUG-12b: Fix với `min-width` hoặc font-size reduction
- [ ] BUG-12c: Test 1200px và 992px viewport

---

### BUG-02: EN/VI dịch chưa đồng bộ
**Mô tả:** Nhiều string hardcoded English, không dùng `@L["key"]`

**Subtasks:**
- [ ] BUG-02a: Audit Products/Index — filter labels, sort options
- [ ] BUG-02b: Audit Builder/Index — xem BUG-03 đã fix chưa
- [ ] BUG-02c: Audit AI chatbot widget — placeholder, labels
- [ ] BUG-02d: Audit Cart, Checkout — field labels
- [ ] BUG-02e: Add missing keys vào `Resources/SharedResource.vi.resx`

---

## SKIP

### BUG-06: Mock stats không thực tế (3500+, 10000+, 98%)
**Lý do:** Không phải bug kỹ thuật. Có thể đổi số nhỏ hơn nếu muốn.

---

## Ghi chú kỹ thuật (cho session mới đọc lại)

- **Order model:** có field `Phone` (required) → search by phone khả thi
- **OrderTrackingController:** public (no auth), hiện yêu cầu cả `orderId` + `phone`
- **OrdersController:** `[Authorize]` toàn bộ, `/Orders/Index` đã sort newest-first ✓
- **Localization:** `@L["key"]` via `IStringLocalizer`, resources ở `Resources/` folder
- **CSS theme:** `data-theme="dark|light"` trên `<html>` element, vars trong `site.css`
- **Floating chat widget:** `Views/Shared/_ChatWidget.cshtml`, inline CSS
- **EF migrations:** dùng `dotnet dotnet-ef` (local tool), KHÔNG dùng global `dotnet-ef`
