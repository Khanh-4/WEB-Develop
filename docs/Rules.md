Làm việc nhóm trên GitHub (đặc biệt khi dự án có nhiều thành phần như C# ASP.NET Core và Python) rất dễ xảy ra xung đột (merge conflict) hoặc ghi đè code của nhau. Dựa trên các tiêu chuẩn phát triển phần mềm và quy trình quản lý mã nguồn, đây là bộ quy tắc (rules) thiết yếu mà hai bạn nên thống nhất:
1. Không bao giờ code trực tiếp trên nhánh main (Branching Strategy)
Nhánh main (hoặc master) phải luôn là nhánh chứa code hoàn chỉnh, ổn định và có thể chạy được (deployable).
Khi mỗi người nhận một task mới, hãy tạo một nhánh mới (branch) cho các thay đổi của mình
.
Cách đặt tên nhánh: Nên thống nhất theo cú pháp loại-công-việc/tên-chức-năng. Ví dụ: bạn A làm cào dữ liệu có thể đặt nhánh là feature/python-scraper, bạn B làm UI có thể đặt là feature/pc-builder-ui hoặc fix/login-bug.
2. Bắt buộc sử dụng Pull Request (PR) và Code Review
Khi hoàn thành tính năng trên nhánh phụ, thay vì tự gộp (merge) vào main, hãy gửi một Pull Request (PR) đính kèm các thay đổi của bạn
.
Thành viên còn lại sẽ đóng vai trò là người đánh giá (Reviewer). Chỉ khi người kia đọc code, hiểu logic và bấm "Approve" thì code mới được phép gộp vào main. Điều này giúp cả hai nắm được tiến độ và kiến trúc hệ thống của nhau.
3. Tích hợp Tự động hóa (CI) và Validation với GitHub Actions
Tích hợp liên tục (Continuous Integration - CI) là một phương pháp thực hành tốt, trong đó mỗi lần đẩy code lên (check-in) đều được xác minh thông qua tự động hóa, cho phép các nhóm phát hiện vấn đề từ sớm
.
Các bạn có thể thiết lập GitHub Actions để tự động thực hiện các công việc: kiểm tra lỗi cú pháp, chạy format code, hoặc build thử project ASP.NET Core mỗi khi có PR mới
.
Trong cấu hình của GitHub, hãy thiết lập chặn không cho phép gộp code (block merging) nếu các bài kiểm tra tự động (validation) bị thất bại
.
4. Sử dụng tệp .gitignore chuẩn mực
Ngay từ lúc khởi tạo dự án, phải thêm tệp .gitignore vào thư mục gốc của repository
.
Tệp này sẽ ngăn chặn việc vô tình đẩy các file rác sinh ra trong quá trình biên dịch (như thư mục bin/, obj/ của C#) hoặc các môi trường ảo (như __pycache__, venv/ của Python) lên GitHub. Việc đẩy các file này lên là nguyên nhân lớn nhất gây ra conflict không đáng có.
5. Lập kế hoạch và theo dõi công việc qua GitHub Issues
Sử dụng tính năng Issues của GitHub để lên kế hoạch và theo dõi tiến độ công việc (Plan and track work)
.
Mỗi khi phát hiện lỗi hoặc cần thêm tính năng (như "Thêm thuật toán tính P/P", "Tạo giao diện giỏ hàng"), hãy mở một Issue mới. Khi tạo PR để giải quyết, chỉ cần gắn tag (ví dụ: Fixes #3), GitHub sẽ tự động đóng Issue đó khi PR được gộp.
6. Quy tắc Phân tách không gian làm việc (Decoupling Workspace) Do nhóm có 2 người làm hai mảng công nghệ khác nhau (Backend/DB bằng Python và Web/UI bằng C#), các bạn nên tổ chức cấu trúc thư mục rõ ràng ngay từ đầu, ví dụ:
Thư mục /scraper: Chỉ chứa code Python. Bạn A toàn quyền quản lý, bạn B không đụng vào.
Thư mục /web: Chứa Project ASP.NET Core. Bạn B chủ yếu làm việc ở đây.
Thư mục /docs: Chứa các file tài liệu Markdown (.md) mà chúng ta đã thống nhất
. Việc phân chia thư mục rõ ràng này giúp tỷ lệ hai người sửa cùng một file (gây ra merge conflict) giảm xuống gần như bằng 0. Nếu có chung các thiết lập cơ sở dữ liệu, hãy đặt chúng trong thư mục /schemas để tiện quản lý
.
Thực hiện tốt các quy tắc này, quá trình làm việc nhóm của hai bạn trên Visual Studio Code sẽ cực kỳ trơn tru, đồng thời tăng đáng kể năng suất của cả hai
.