# Manual Flow Test — TechSpecs

> **URL production:** https://web-develop-production.up.railway.app  
> **Tài khoản admin:** `duylamasd1995@gmail.com`  
> **Cách ghi lỗi:** URL + hành động + kết quả thực tế + screenshot nếu có  
> **Cập nhật:** 2026-06-09 (Session 16 — full feature audit)

---

## 1. AUTH

### 1.1 Đăng ký
- [ ] `/Account/Register` → điền FullName + email mới + password ≥8 ký tự có số → Submit
- [ ] Kết quả: redirect `/`, header hiển thị tên (không phải email), cart badge xuất hiện
- [ ] Lỗi expected: email đã tồn tại → thông báo lỗi ngay form, không redirect

### 1.2 Đăng nhập / Đăng xuất
- [ ] `/Account/Login` → đăng nhập đúng → redirect `/`, tên hiển thị trên header
- [ ] Sai mật khẩu → "Email hoặc mật khẩu không đúng", ở lại trang login
- [ ] Mở dropdown avatar (góc phải header) → bấm "Đăng xuất" → redirect `/`, header trở về trạng thái guest

### 1.3 Google OAuth
- [ ] Bấm "Đăng nhập với Google" → chọn tài khoản Google → redirect `/` đã đăng nhập

### 1.4 Quên mật khẩu
- [ ] `/Account/ForgotPassword` → nhập email → nhận email chứa link reset (kiểm tra Gmail)
- [ ] Click link → `/Account/ResetPassword` → đặt password mới → đăng nhập được

### 1.5 Profile
- [ ] `/Account/Profile` → đổi FullName → Lưu → header cập nhật tên mới ngay

### 1.6 Bảo vệ route
- [ ] Khi chưa đăng nhập, truy cập `/Cart` / `/Orders` / `/Orders/Checkout` → redirect `/Account/Login`
- [ ] Truy cập `/Admin/Dashboard` khi không phải Admin → redirect `/Account/Login`

---

## 2. TRANG CHỦ

### 2.1 Layout & Sections
- [ ] Hero carousel load (3 slides, tự chạy, có nút prev/next)
- [ ] Strip lợi ích bên dưới hero (4 icon: Bảo hành / Giao nhanh / Trả hàng / Tư vấn)
- [ ] Section "Sản phẩm nổi bật" hiện sản phẩm từng category (CPU / GPU / RAM / …)
- [ ] Section Flash Sale (nếu admin đã tạo): hiện sản phẩm + countdown timer
- [ ] Hover product card → nút "Thêm vào giỏ" + "So sánh" hiện, border tím sáng lên

### 2.2 Header & Search
- [ ] Search bar: gõ "RTX" → live-search dropdown gợi ý ≤5 sản phẩm
- [ ] Bấm kết quả → vào trang detail đúng sản phẩm
- [ ] Gõ `/` trên bàn phím → focus search bar (keyboard shortcut)
- [ ] Mini-cart: hover icon giỏ (desktop) → drawer hiện với danh sách items

### 2.3 Quick Quote
- [ ] Kéo xuống section "Nhận báo giá nhanh" → điền SĐT + ngân sách → "Gửi yêu cầu"
- [ ] Kết quả: toast thành công + email thông báo đến Gmail admin

### 2.4 Recently Viewed
- [ ] Vào xem 2–3 trang detail sản phẩm → back về trang chủ → section "Bạn vừa xem" xuất hiện

### 2.5 Mobile (thu nhỏ < 768px)
- [ ] Bottom navigation bar hiện ở mép dưới (Home / Danh mục / Giỏ / Tài khoản)
- [ ] Tap search bar → full-screen search overlay mở
- [ ] Product card block vuốt ngang được (scroll-snap)

### 2.6 Theme & Ngôn ngữ
- [ ] Bấm icon mặt trăng/mặt trời → dark/light theme chuyển, persist khi reload
- [ ] Bấm VI/EN → toàn bộ label đổi ngôn ngữ

---

## 3. TRANG GIỚI THIỆU & LIÊN HỆ

### 3.1 Giới thiệu `/Home/About`
- [ ] Page load: hero + 3 tx-card (Sứ mệnh / Đội ngũ / Cam kết)
- [ ] Stats row: 3,500+ linh kiện / 10,000+ cấu hình / 98% hài lòng / 24/7 hỗ trợ
- [ ] Feature grid 6 ô (AI Builder / Kiểm tra tương thích / So sánh / Chia sẻ / Giao hàng / Cập nhật giá)
- [ ] CTA cuối: nút "Bắt đầu build PC" → `/Builder`, nút "Liên hệ tư vấn" → `/Home/Contact`

### 3.2 Liên hệ `/Home/Contact`
- [ ] Hiện địa chỉ / hotline / email / giờ làm việc
- [ ] Form: điền Họ tên + Email + Nội dung → bấm "Gửi tin nhắn" → toast "Đã gửi"
- [ ] 3 quick-link cards phía dưới (PC Builder / Bảo hành / Theo dõi đơn)

---

## 4. SẢN PHẨM

### 4.1 Danh sách `/Products`
- [ ] Default: hiện sản phẩm xen kẽ tất cả danh mục
- [ ] Bấm category pill (CPU / GPU / RAM / MB / PSU / Case / Storage / Cooler) → filter đúng
- [ ] Sort: Giá tăng / Giá giảm / Tên A-Z hoạt động đúng
- [ ] Filter giá: nhập min/max → chỉ hiện sản phẩm trong khoảng giá
- [ ] Filter brand: tick 1 brand → chỉ hiện brand đó
- [ ] Active filter chips: khi filter đang bật → chip hiện phía trên grid, bấm × để xóa
- [ ] Skeleton loading: khi chuyển category → shimmer hiện trước khi có data
- [ ] Hover card → spec overlay slide lên (tên + giá + specs nhanh + nút Add to Cart)
- [ ] Compare bar: tick 2 sản phẩm cùng category → bar sticky "So sánh (2)" ở dưới → bấm → trang compare

### 4.2 Quick View
- [ ] Hover card → icon mắt xuất hiện → click → modal hiện ảnh + specs + "Thêm vào giỏ"

### 4.3 Product Detail `/Products/Detail/{cat}/{id}`
- [ ] Ảnh lớn, full specs table, badge tồn kho (Còn hàng / Còn ít / Hết hàng)
- [ ] Killer Specs badges: chip highlight spec nổi bật (DDR5 / PCIe 5.0 / …)
- [ ] Sticky Add-to-Cart bar: scroll xuống → bar ghim ở đáy màn hình
- [ ] Spec tooltips: hover tên spec → tooltip giải thích ngắn
- [ ] FPS Estimator (trang GPU): bảng FPS ước tính theo game phổ biến
- [ ] YouTube embed: nếu có video sản phẩm → embed phát được
- [ ] Price history chart: Chart.js hiện biểu đồ (có thể trống nếu chưa có data)
- [ ] Cross-sell: phần "Sản phẩm liên quan" ở dưới
- [ ] "Thêm vào giỏ" → fly animation + badge tăng
- [ ] "Thêm vào Builder" → redirect Builder với sản phẩm đã chọn sẵn
- [ ] Review section: đăng nhập → gửi review (1–5 sao + text + ảnh tùy chọn) → hiện ngay
- [ ] Installment Calculator: bấm "Tính trả góp" → modal hiện số tiền/tháng đúng

### 4.4 Compare `/Products/Compare?category=cpu&ids=X,Y`
- [ ] Bảng side-by-side 2 sản phẩm
- [ ] Ô tốt hơn: highlight xanh + trophy icon; ô tệ hơn: đỏ

---

## 5. GIỎ HÀNG & THANH TOÁN

### 5.1 Giỏ hàng `/Cart`
- [ ] Thêm 2–3 sản phẩm khác nhau → vào `/Cart` → hiện đúng item + đơn giá + tổng
- [ ] Tăng/giảm số lượng → tổng tiền cập nhật ngay (không reload)
- [ ] Xóa 1 item → row biến mất, tổng cập nhật
- [ ] Giỏ trống → hiện "Giỏ hàng trống" + nút "Xem sản phẩm"

### 5.2 Checkout `/Orders/Checkout`
- [ ] Bấm "Thanh toán" từ giỏ → vào Checkout
- [ ] Submit khi chưa điền tên / SĐT / địa chỉ → lỗi validation, không redirect
- [ ] Nhập coupon `TECHSPECS10` → hiện giảm 10%
- [ ] Chọn phương thức: COD / Chuyển khoản / VNPay / MoMo / ZaloPay
- [ ] Submit đủ thông tin → redirect `/Orders/Confirmation/{id}`, giỏ hàng trống

### 5.3 Xác nhận đơn hàng `/Orders/Confirmation/{id}`
- [ ] Hiện: mã đơn + tên người nhận + địa chỉ + danh sách SP + tổng tiền
- [ ] Nút "Xem đơn hàng" → `/Orders/Detail/{id}`

### 5.4 Lịch sử đơn hàng `/Orders`
- [ ] Danh sách đơn hàng của tài khoản, sắp theo mới nhất
- [ ] Click vào đơn → `/Orders/Detail/{id}` → đúng items + tổng
- [ ] Đơn "Chờ xác nhận" → nút "Hủy đơn" → status đổi thành "Đã huỷ"

### 5.5 Order Tracking `/OrderTracking`
- [ ] Nhập mã đơn hàng (không cần đăng nhập) → hiện tên người nhận + trạng thái + timeline

---

## 6. PC BUILDER `/Builder`

### 6.1 Chọn linh kiện
- [ ] Chọn CPU Intel → Mainboard chỉ hiện socket LGA1700 / AM5 tương ứng
- [ ] Chọn Mainboard DDR5 → RAM chỉ hiện DDR5
- [ ] Chọn GPU dài → Case chỉ hiện case có MaxVGALength ≥ chiều dài GPU
- [ ] Chọn CPU + GPU → PSU tự tính TDP × 1.3, chỉ hiện PSU đủ công suất
- [ ] **PSU Power Breakdown** panel: hiện CPU TDP + GPU TDP + tổng watt ước tính

### 6.2 Lưu & Chia sẻ
- [ ] Chọn ≥3 linh kiện → "Lưu Build" → đặt tên → lưu thành công (cần đăng nhập)
- [ ] `/Build/MyBuilds` → danh sách builds, có nút "Mở trong Builder" + "Chia sẻ" + "Xóa"
- [ ] Bấm "Chia sẻ" → copy link `/Build/Share/{token}` → mở tab ẩn danh → xem được build

### 6.3 So sánh Build (Radar Chart)
- [ ] Build ≥3 linh kiện → "Snapshot A" → thay vài linh kiện → "So sánh với A" → radar chart 5-axis hiện

### 6.4 Export PDF
- [ ] Chọn đủ linh kiện → "Xuất PDF" → file PDF tải về với bảng giá + thông số

### 6.5 AI Chat
- [ ] Mở chat widget → nhập "Build PC gaming 20 triệu" → AI trả về cấu hình + nút "Xem trong Builder"
- [ ] Bấm link → Builder load cấu hình từ AI, chat preset áp dụng đúng

---

## 7. CỘNG ĐỒNG & WISHLIST

### 7.1 Community `/Build/Community`
- [ ] Hiện danh sách public builds, sắp theo upvote
- [ ] Bấm "Upvote" → số tăng (cần đăng nhập)
- [ ] Bấm "Publish" trên build cá nhân → build hiện trong Community

### 7.2 Wishlist `/Wishlist`
- [ ] Trang Detail sản phẩm → bấm icon tim → vào Wishlist
- [ ] `/Wishlist` → danh sách, bấm "Thêm vào giỏ" → item vào Cart

---

## 8. BẢO HÀNH

### 8.1 Tra cứu public `/Warranty`
- [ ] Nhập SĐT (không cần đăng nhập) → hiện danh sách bảo hành theo SĐT

### 8.2 Bảo hành cá nhân `/Account/Warranties`
- [ ] Đăng nhập → vào `/Account/Warranties` → hiện bảo hành của đơn mình đã mua (sau khi admin xác nhận đơn)

---

## 9. ADMIN PANEL

> Đăng nhập bằng `duylamasd1995@gmail.com` trước khi test mục này.

### 9.1 Dashboard `/Admin/Dashboard`
- [ ] Hiện 4 stat cards: Người dùng / Đơn hàng / Doanh thu / Sản phẩm
- [ ] Quick-nav grid: 10 ô shortcut (Sản phẩm / Đơn hàng / Flash Sale / Coupon / Bundle / Benchmark / Báo giá / Bảo hành / Đánh giá / Người dùng)
- [ ] Bảng "Đơn hàng gần đây" và biểu đồ "Sản phẩm theo danh mục" hiển thị đúng

### 9.2 Products `/Admin/Products`
- [ ] Chọn category tab → list sản phẩm đúng category
- [ ] Click Edit 1 sản phẩm → `/Admin/EditProduct?category=cpu&id=X` → đổi giá → Save
- [ ] Giá mới hiện ngoài trang `/Products` sau khi save
- [ ] Xóa 1 sản phẩm → biến mất khỏi danh sách

### 9.3 Orders `/Admin/Orders`
- [ ] Filter status "Chờ xác nhận" → chỉ hiện đúng status đó
- [ ] Đổi status (dropdown) → AJAX cập nhật thành công (toast xanh "Đã cập nhật")
- [ ] Bấm icon mắt → `/Admin/OrderDetail/{id}` (KHÔNG mở tab mới)

### 9.4 Order Detail `/Admin/OrderDetail/{id}`
- [ ] Hiện: danh sách SP (tên + ảnh + SL + giá + thành tiền), tổng tiền, discount nếu có
- [ ] Panel bên phải: tên người nhận + SĐT + địa chỉ + ghi chú + email tài khoản + phương thức thanh toán
- [ ] 7 nút trạng thái: bấm "Đang lắp ráp" → active highlight (gradient), toast thành công
- [ ] Nút "Quay lại danh sách" → về `/Admin/Orders`
- [ ] Nút "Xem trang khách hàng" → mở `/Orders/Detail/{id}` ở tab mới

### 9.5 Flash Sales `/Admin/FlashSales`
- [ ] Tạo flash sale mới (`/Admin/CreateFlashSale`): chọn category + nhập Product ID + tên + giá + % giảm + số lượng + thời gian
- [ ] Flash sale hiện trong danh sách → bấm Toggle On/Off → trạng thái thay đổi
- [ ] Ra trang chủ → section Flash Sale hiện sản phẩm + countdown timer (khi đang ON)
- [ ] Bấm Delete → xác nhận → flash sale biến mất

### 9.6 Coupons `/Admin/Coupons`
- [ ] Tạo coupon `TEST20` giảm 20% (`/Admin/CreateCoupon`)
- [ ] Vào Checkout → nhập `TEST20` → áp dụng discount 20%
- [ ] Toggle Off coupon → Checkout báo coupon không hợp lệ
- [ ] Xóa coupon

### 9.7 Bundles `/Admin/Bundles`
- [ ] Tạo bundle với ≥2 category items + discount % (`/Admin/CreateBundle`)
- [ ] Thêm đúng combo vào giỏ → row "Bundle discount" hiện trong Cart
- [ ] Toggle Off / Xóa bundle

### 9.8 Benchmarks `/Admin/Benchmarks`
- [ ] Thêm benchmark mới: điền tên CPU/GPU + điểm đơn luồng + đa luồng + gaming
- [ ] Edit benchmark → điểm thay đổi
- [ ] Xóa benchmark

### 9.9 Reviews `/Admin/Reviews`
- [ ] Hiện danh sách tất cả đánh giá (phân trang)
- [ ] Filter theo số sao (1–5) → chỉ hiện đúng số sao
- [ ] Filter theo category (CPU / GPU…) → đúng
- [ ] Bấm Delete review → xác nhận → biến mất

### 9.10 Users `/Admin/Users`
- [ ] Search email → tìm đúng user
- [ ] Bấm "↑ Admin" → user có role Admin → bấm "↓ Customer" → về Customer
- [ ] Phân trang hoạt động đúng (có >30 user)

### 9.11 Warranties `/Admin/Warranties`
- [ ] Search SĐT → hiện bảo hành đúng SĐT
- [ ] Bảo hành được tự tạo khi admin confirm đơn hàng → kiểm tra: confirm 1 đơn → SĐT của đơn đó xuất hiện trong danh sách bảo hành

### 9.12 Quote Requests `/Admin/QuoteRequests`
- [ ] Hiện danh sách leads từ form "Nhận báo giá nhanh" trên homepage
- [ ] Filter "Chưa liên hệ" → chỉ hiện leads chưa xử lý
- [ ] Bấm ✓ → status đổi "Đã liên hệ", row mờ đi
- [ ] Bấm 🗑 → lead biến mất

### 9.13 Scraper `/Admin/Scraper`
- [ ] Page load không lỗi
- [ ] Nút "Chạy scraper" hiện (không cần test thực sự — chỉ kiểm tra UI load)

---

## 10. TRANG PHỤ

- [ ] `/Home/Privacy` → page load, không lỗi 500
- [ ] `/Home/About` → hero + stat cards + feature grid + CTA hiển thị đúng
- [ ] `/Home/Contact` → contact info + form + quick-link cards hiển thị đúng
- [ ] `/Build/Compare` → trang so sánh 2 builds load đúng (nếu có builds)
- [ ] 404: truy cập `/xyz/khongcotrang` → trang lỗi tùy chỉnh (không phải trang trắng)

---

## 11. UX / KEYBOARD

- [ ] `Esc` → đóng modal / overlay đang mở
- [ ] Phím `←` `→` trong Quick View modal → chuyển sản phẩm
- [ ] **Purchase Toast**: chờ 15 giây trên trang chủ → pop-up "Anh K. vừa đặt mua…" xuất hiện góc dưới-trái → tự đóng sau 5 giây
- [ ] **Live Chat**: 2 icon (Zalo + Messenger) cố định ở góc dưới-phải
- [ ] **Membership badge**: vào Profile sau khi có đơn hàng được Confirm → hiện cấp thành viên (Silver / Gold / Diamond) + điểm loyalty

---

## CÁCH GHI LỖI

```
[Mục] Tên flow
URL: https://web-develop-production.up.railway.app/...
Hành động: làm gì
Kết quả thực tế: thấy gì
Kết quả mong đợi: nên thấy gì
Screenshot: (đính kèm nếu có)
```
