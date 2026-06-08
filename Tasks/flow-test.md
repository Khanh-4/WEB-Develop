# Manual Flow Test — TechSpecs

> URL production: https://web-develop-production.up.railway.app  
> Tài khoản admin: `duylamasd1995@gmail.com`  
> Ghi chú lỗi: copy URL + mô tả ngắn + screenshot nếu có

---

## 1. Auth

### 1.1 Đăng ký
- [ ] Vào `/Account/Register`, điền email mới + password ≥8 ký tự có số
- [ ] Submit → redirect về trang chủ, navbar hiển thị tên (không phải email)
- [ ] Lỗi expected: email đã tồn tại → hiện thông báo lỗi ngay form

### 1.2 Đăng nhập / Đăng xuất
- [ ] Vào `/Account/Login`, đăng nhập bằng email + password
- [ ] Logout → navbar trở về trạng thái chưa đăng nhập
- [ ] Sai password → hiện "Email hoặc mật khẩu không đúng"

### 1.3 Google OAuth
- [ ] Bấm "Đăng nhập với Google" → chọn tài khoản Google → redirect về trang chủ đã đăng nhập

### 1.4 Quên mật khẩu
- [ ] `/Account/ForgotPassword` → nhập email → nhận email reset (kiểm tra Gmail)
- [ ] Click link trong email → đặt password mới → đăng nhập được bằng password mới

### 1.5 Profile
- [ ] `/Account/Profile` → đổi FullName → lưu → navbar cập nhật tên mới ngay

---

## 2. Trang chủ

- [ ] Homepage load đủ: hero carousel, flash sale (nếu có), các section CPU/GPU/RAM/SSD/MB
- [ ] Hover product card → border tím + nút "Thêm vào giỏ" / "So sánh" slide lên
- [ ] Bấm "Thêm vào giỏ" từ homepage → toast xác nhận + badge giỏ hàng tăng
- [ ] Hover icon giỏ hàng (desktop) → mini-cart dropdown hiện ra
- [ ] **Recently Viewed**: vào xem 2–3 sản phẩm → back về trang chủ → section "Bạn vừa xem" hiện
- [ ] **Search bar** (desktop): gõ "RTX" → gợi ý live search xuất hiện
- [ ] **Quick Quote form**: điền SĐT + ngân sách → "Gửi yêu cầu" → toast thành công + email đến Gmail admin

### Mobile (thu nhỏ cửa sổ < 768px)
- [ ] Bottom navigation bar hiện ở đáy màn hình (4 nút: Home / Danh mục / Giỏ / Tài khoản)
- [ ] Tap vào search bar → full-screen search overlay mở ra
- [ ] Product grid chuyển sang vuốt ngang (scroll-snap)

---

## 3. Sản phẩm

### 3.1 Trang danh sách `/Products`
- [ ] Load mặc định: hiển thị sản phẩm "Tất cả" xen kẽ các danh mục
- [ ] Bấm category pill (CPU / GPU / RAM…) → filter đúng danh mục
- [ ] Sort: Giá tăng / giảm / Tên hoạt động đúng
- [ ] Filter giá: nhập min 5.000.000 max 15.000.000 → chỉ hiện sản phẩm trong khoảng
- [ ] Filter brand: tick một brand → chỉ hiện brand đó
- [ ] **Active filter chips**: khi filter đang bật → hiện chip "CPU · Intel ×" phía trên grid, bấm × để xóa filter
- [ ] Skeleton loading: khi chuyển trang / category → hiện shimmer trước khi có data
- [ ] Hover card → spec overlay slide lên (tên + giá + specs nhanh)
- [ ] **Compare**: tick 2 sản phẩm cùng category → sticky bar "So sánh (2)" xuất hiện ở dưới → bấm "So sánh" → mở trang compare

### 3.2 Quick View
- [ ] Hover product card → icon mắt xuất hiện → click → modal quick view mở ra với ảnh + specs + nút Add to Cart

### 3.3 Product Detail
- [ ] Click vào tên sản phẩm → trang detail `/Products/Detail/{cat}/{id}`
- [ ] Ảnh lớn, full specs, badge "Còn hàng" / "Còn ít" / "Hết hàng"
- [ ] **Killer Specs badges**: chip nổi bật (VD: "DDR5", "PCIe 5.0") hiện đúng
- [ ] **Sticky Add-to-Cart bar**: scroll xuống → bar ghim ở dưới màn hình
- [ ] **Spec tooltips**: hover vào tên spec → tooltip giải thích ngắn
- [ ] **FPS Estimator** (GPU): trang detail GPU → bảng FPS ước tính theo game
- [ ] **YouTube embed**: nếu sản phẩm có video → embed hiển thị
- [ ] **Price history chart**: Chart.js hiện lịch sử giá (có thể trống nếu chưa có data)
- [ ] **Cross-sell**: gợi ý linh kiện phù hợp phía dưới
- [ ] Bấm "Thêm vào giỏ" → fly animation + badge tăng
- [ ] Bấm "Thêm vào Builder" → redirect sang Builder với sản phẩm đã chọn
- [ ] **Đánh giá**: cuộn xuống phần Reviews → gửi review (cần đăng nhập) → hiện ngay

### 3.4 Compare
- [ ] `/Products/Compare?category=cpu&ids=1,2` → bảng so sánh side-by-side
- [ ] Ô tốt hơn: highlight xanh + trophy, ô tệ hơn: đỏ

---

## 4. Giỏ hàng & Thanh toán

### 4.1 Giỏ hàng `/Cart`
- [ ] Thêm 2–3 sản phẩm khác nhau → vào `/Cart` → hiển thị đúng item + giá
- [ ] Tăng/giảm số lượng → tổng tiền cập nhật ngay
- [ ] Xóa 1 item → danh sách cập nhật
- [ ] Giỏ trống → hiển thị "Giỏ hàng trống" + nút quay về shop

### 4.2 Checkout
- [ ] Bấm "Thanh toán" → trang `/Orders/Checkout`
- [ ] Điền thiếu trường bắt buộc → hiện lỗi validation (không được redirect)
- [ ] Nhập mã giảm giá `TECHSPECS10` → áp dụng 10% discount
- [ ] Chọn phương thức thanh toán: COD / VNPay / MoMo / ZaloPay
- [ ] Submit → trang xác nhận đơn hàng, giỏ hàng trở về trống

### 4.3 Lịch sử đơn hàng
- [ ] `/Orders` → danh sách đơn hàng của tài khoản
- [ ] Click vào đơn → `/Orders/Detail/{id}` → đúng items + tổng tiền
- [ ] Đơn "Chờ xử lý" → nút "Hủy đơn" → status đổi thành "Đã hủy"

### 4.4 Order Tracking (public)
- [ ] `/OrderTracking` → nhập mã đơn hàng → hiện trạng thái (không cần đăng nhập)

---

## 5. PC Builder

### 5.1 Chọn linh kiện
- [ ] `/Builder` → chọn CPU → danh sách Mainboard tự filter đúng socket
- [ ] Chọn Mainboard → RAM filter đúng loại DDR4/DDR5
- [ ] Thêm GPU → PSU tự tính wattage tối thiểu, lọc PSU đủ công suất
- [ ] Chọn Case → GPU lọc theo MaxVGALength
- [ ] **PSU Power Breakdown**: panel bên trái hiển thị CPU TDP + GPU TDP + tổng

### 5.2 Lưu & Chia sẻ Build
- [ ] Chọn ≥3 linh kiện → nút "Lưu Build" → điền tên → lưu thành công
- [ ] `/Build/MyBuilds` → danh sách builds đã lưu
- [ ] Bấm "Chia sẻ" → copy link `/Build/Share/{token}` → mở link ở tab ẩn danh → xem được build

### 5.3 So sánh Build
- [ ] Build hiện tại ≥3 linh kiện → bấm "Snapshot A" → thay đổi 1–2 linh kiện → bấm "So sánh với A" → radar chart 5-axis

### 5.4 AI Chat
- [ ] Mở chat widget → nhập "Build PC gaming 20 triệu" → AI trả về cấu hình + link "Xem trong Builder"
- [ ] Bấm link → Builder load cấu hình từ AI

### 5.5 Export PDF
- [ ] Chọn đủ linh kiện → bấm "Xuất PDF" → file PDF tải xuống với bảng giá

---

## 6. Cộng đồng & Wishlist

### 6.1 Community
- [ ] `/Community` → danh sách public builds sắp theo upvote
- [ ] Bấm "Publish" trên build của mình → build hiện trong Community
- [ ] Upvote build của người khác → số upvote tăng

### 6.2 Wishlist
- [ ] Trang Detail → bấm icon tim → sản phẩm vào Wishlist
- [ ] `/Wishlist` → danh sách, bấm "Thêm vào giỏ" → vào cart

---

## 7. Bảo hành

- [ ] `/Warranty` → nhập SĐT hoặc serial → hiện danh sách bảo hành
- [ ] `/Account/Warranties` (đăng nhập) → bảo hành của tài khoản mình

---

## 8. Admin Panel (dùng tài khoản admin)

### 8.1 Dashboard `/Admin/Dashboard`
- [ ] Hiện đúng số: tổng user, đơn hàng, doanh thu, sản phẩm theo danh mục

### 8.2 Products
- [ ] `/Admin/Products` → chọn category → list sản phẩm
- [ ] Edit 1 sản phẩm → đổi giá → Save → giá cập nhật ngoài trang web
- [ ] Xóa 1 sản phẩm → biến mất khỏi danh sách

### 8.3 Orders
- [ ] `/Admin/Orders` → filter theo status "Chờ xử lý"
- [ ] Đổi status đơn → "Đang giao" → reload thấy thay đổi

### 8.4 Báo giá nhanh
- [ ] `/Admin/QuoteRequests` → danh sách leads từ form "Nhận báo giá nhanh"
- [ ] Bấm ✓ (Đã liên hệ) → row mờ đi, badge đổi sang "Đã liên hệ"
- [ ] Bấm 🗑 xóa → lead biến mất

### 8.5 Flash Sales
- [ ] `/Admin/FlashSales` → tạo flash sale mới với 1 sản phẩm + giảm 20% + thời gian hôm nay
- [ ] Ra trang chủ → section Flash Sale hiện sản phẩm + countdown timer
- [ ] Toggle off → Flash Sale ẩn

### 8.6 Coupons
- [ ] `/Admin/Coupons` → tạo coupon `TEST20` giảm 20%
- [ ] Vào checkout → nhập `TEST20` → áp dụng thành công

### 8.7 Bundles
- [ ] `/Admin/Bundles` → tạo bundle CPU + MB + RAM với giảm 5%
- [ ] Thêm đúng combo vào giỏ → row discount bundle hiện trong cart

### 8.8 Benchmarks
- [ ] `/Admin/Benchmarks` → thêm benchmark cho "Intel i5-13600K"
- [ ] Vào trang detail CPU đó → benchmark hiển thị (nếu có UI)

### 8.9 Users
- [ ] `/Admin/Users` → tìm email → toggle role Admin/Customer

### 8.10 Bảo hành
- [ ] `/Admin/Warranties` → search SĐT → hiện bảo hành đúng

---

## 9. Ngôn ngữ & Theme

- [ ] Bấm nút VI/EN → toàn bộ label chuyển ngôn ngữ
- [ ] Bấm sun/moon → theme sáng/tối chuyển đổi + persist khi reload

---

## 10. UX nhỏ

- [ ] Gõ `/` trên bàn phím → focus vào search bar
- [ ] `Esc` → đóng modal đang mở
- [ ] Phím `←` `→` trong Quick View → chuyển sản phẩm
- [ ] Installment Calculator: bấm "Tính trả góp" trên Detail/Checkout → modal hiện đúng số tiền/tháng
- [ ] **Purchase toast**: chờ 15 giây → pop-up "Anh K. vừa đặt mua…" xuất hiện ở góc dưới-trái → tự đóng sau 5s
- [ ] Live chat: icon Zalo + Messenger hiện ở góc dưới-phải

---

## Cách ghi lỗi

Khi phát hiện lỗi, gửi lại theo format:

```
[Mục] Tên flow
URL: https://...
Hành động: làm gì
Kết quả thực tế: thấy gì
Kết quả mong đợi: nên thấy gì
Screenshot: (nếu có)
```
