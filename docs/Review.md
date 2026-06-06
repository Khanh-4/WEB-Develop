# TechSpecs — PC/Laptop Builder System

Dự án **TechSpecs** là hệ thống thương mại và tự cấu hình máy tính (PC/Laptop Builder) giải quyết bài toán tương thích phần cứng cho nhiều tệp người dùng khác nhau.

---

## 1. Kiến trúc Hệ thống & Tech Stack

> Tối ưu cho Server Free & Visual Studio Code

### Tầng Giao diện (Frontend)

Sử dụng **HTML, CSS, JavaScript, Bootstrap** và **jQuery/AJAX** để tạo trải nghiệm chọn linh kiện mượt mà, cập nhật động thông số và giá tiền mà không cần tải lại trang.

### Tầng Ứng dụng (Backend)

Dùng **ASP.NET Core MVC (C#)** lập trình trực tiếp trên Visual Studio Code.

- Quản lý tài khoản: **ASP.NET Core Identity**
- Deploy: **Railway** (cung cấp sẵn tên miền phụ miễn phí để nộp đồ án)

### Tầng Dữ liệu & Tìm kiếm

Sử dụng **PostgreSQL** host trên **Supabase** (miễn phí).

Các bảng linh kiện chuyên biệt:

| Bảng             | Mô tả          |
|------------------|----------------|
| `cpu`            | Vi xử lý       |
| `motherboard`    | Bo mạch chủ    |
| `memory`         | RAM            |
| `storage`        | Ổ cứng         |
| `video_card`     | Card đồ họa    |
| `case_enclosure` | Case máy tính  |
| `power_supply`   | Nguồn (PSU)    |
| `cpu_cooler`     | Tản nhiệt CPU  |

- ORM: **Entity Framework Core (Npgsql)**
- Tìm kiếm mờ: extension **`pg_trgm`** — vẫn tìm ra kết quả đúng dù người dùng gõ sai tên linh kiện

### Tầng Thu thập Dữ liệu (Data Acquisition)

Dùng **Python** kết hợp **Beautiful Soup**, **Selenium** và **SQLAlchemy** để tự động cào, làm sạch và chuẩn hóa dữ liệu phần cứng từ các trang bán lẻ.

### Tích hợp Đám mây & AI

- Lưu trữ ảnh linh kiện: **Firebase Storage** hoặc **GCP Cloud Storage**
- AI phân tích ngôn ngữ tự nhiên: **Google Gemini API** (fallback sang **Groq** hoặc **OpenRouter**)

---

## 2. Các Tính Năng Cốt Lõi

### Thuật toán Lọc Tương thích Đa tầng (Compatibility Engine)

Áp dụng quy trình lọc nhiều bước (**multi-pass refinement**) để đưa các ràng buộc cứng vào truy vấn:

- Mainboard khớp Socket CPU
- Case vừa với kích thước VGA
- Mainboard hỗ trợ đúng chuẩn RAM
- **PSU** tự động tính dựa trên tổng TDP của CPU + GPU cộng thêm **30% bù tải an toàn**

### Điểm Hiệu Năng P/P (Performance Score)

Chấm điểm cấu hình theo công thức:

```text
Score = Hiệu năng / Giá tiền
```

Giúp người dùng tối ưu hóa ngân sách.

### Chế độ Skilled Builder (Người có kinh nghiệm)

- Giao diện từng bước (**step-by-step**)
- Tự động lưu trạng thái lựa chọn
- Lọc và hiển thị các linh kiện tương thích ở các bước tiếp theo

### Chế độ Beginner Builder (Người mới)

- Giao diện trò chuyện (**chat-based**) tích hợp AI trợ lý ảo
- Người dùng chỉ cần nhập **ngân sách** và **nhu cầu** (chơi game, làm đồ họa, ...)
- AI tự động phân tích và đưa ra cấu hình trọn bộ đề xuất
- Xóa bỏ rào cản thiếu kiến thức chuyên môn về phần cứng

### Tính Năng Thương mại & Quản trị

- Giỏ hàng, thanh toán, quản lý người dùng và đơn hàng
- **Admin Dashboard**: quản lý linh kiện (CRUD), nút kích hoạt script Python để đồng bộ dữ liệu mới

---

## 3. Phân Công Công Việc

> Nhóm 2 người

### Thành viên 1 — Data Python & Database Core

- Viết script Python cào dữ liệu
- Thiết kế lược đồ CSDL trên Supabase (PostgreSQL)
- Viết các truy vấn SQL/LINQ, cấu hình `pg_trgm`
- Triển khai thuật toán lọc tương thích và tính điểm P/P

### Thành viên 2 — ASP.NET Core Web & Frontend UI/UX

- Dựng khung project MVC trên VS Code, kết nối Entity Framework Core
- Xây dựng giao diện Frontend (Skilled Builder & Beginner Builder) bằng Bootstrap/AJAX
- Gọi Gemini API, xử lý Giỏ hàng
- Deploy toàn bộ hệ thống web lên **Railway**
