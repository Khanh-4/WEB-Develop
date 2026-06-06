# ✅ Completed Tasks

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-03

---

## 1. Project Setup & Infrastructure

- [x] Khởi tạo repo, cấu trúc thư mục `/web`, `/scraper`, `/docs`
- [x] Scaffold ASP.NET Core MVC project (.NET 8) tại `/web`
- [x] Cài packages: EF Core 8, Npgsql 8, Identity, Google OAuth
- [x] Cài `dotnet-ef` v8 local tool (`.config/dotnet-tools.json`)
- [x] Tạo Python venv tại `/scraper/venv`, cài `requirements.txt`
- [x] Cập nhật `.gitignore` (exclude `bin/`, `obj/`, `.env`, `venv/`, `appsettings.Development.json`)

---

## 2. Database & Migrations

- [x] Kết nối Supabase PostgreSQL (Session Pooler, port 5432)
- [x] Tạo `AppDbContext` kế thừa `IdentityDbContext<ApplicationUser>`
- [x] **Migration 1 — InitialIdentity**: tạo 7 bảng ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, v.v.)
- [x] **Migration 2 — HardwareSchema**: tạo 8 bảng linh kiện:
  - `cpu` — Socket, CoreCount, TDP, ApproximatePerformance
  - `motherboard` — SocketCompatibility, FormFactor, MemoryCompatibility
  - `memory` — Type (DDR4/5), Capacity, Speed
  - `video_card` — VRAM, Length, TDP, ApproximatePerformance
  - `power_supply` — Wattage, Efficiency, Modular
  - `case_enclosure` — FormFactorSupport, MaxVGALength
  - `storage` — Type, Capacity, Interface, ReadSpeed, WriteSpeed
  - `cpu_cooler` — SocketCompatibility, MaxTDP, Height, Type

---

## 3. Authentication

- [x] ASP.NET Core Identity — Login/Register bằng email + password
- [x] Google OAuth 2.0 — nút "Continue with Google"
- [x] `AccountController` — Login, Register, ExternalLogin, ExternalLoginCallback, Logout
- [x] Views `Login.cshtml`, `Register.cshtml` — glassmorphism style
- [x] Seed roles `Admin` / `Customer` tự động khi app khởi động
- [x] Navbar cập nhật động: Login/SignUp khi chưa đăng nhập, dropdown tên user khi đã đăng nhập

---

## 4. Data Scraper (Python)

- [x] Xác định đúng URLs Phong Vũ cho 9 categories:
  - `/c/cpu`, `/c/mainboard-bo-mach-chu`, `/c/ram`, `/c/vga-card-man-hinh`
  - `/c/psu-nguon-may-tinh`, `/c/case`, `/c/o-cung-ssd`, `/c/o-cung-hdd`, `/c/tan-nhiet`
- [x] `scrapers/phongvu.py` — static HTML scraper (không cần Selenium/Chrome)
  - Selectors: `div.product-card`, `.att-product-card-title`, `.att-product-detail-retail-price`
  - Fetch detail page để lấy specs (socket, cores, clock, TDP, v.v.)
  - Fallback: extract thông tin từ tên sản phẩm nếu detail page fail
- [x] `processors/normalizer.py` — chuẩn hóa đơn vị: GB, MHz, GHz, W, mm, VNĐ
- [x] `scoring/performance.py` — heuristic score cho CPU và GPU:
  - CPU: `(cores^0.75) × boost_clock × 10`
  - GPU: tier lookup table (RTX 4090 = 950, ..., GTX 1650 = ~250) + VRAM bonus
- [x] `main.py` — orchestrator, upsert (skip duplicate by Name)
- [x] **Chạy thành công**: 3,579 sản phẩm trong DB
  - CPU: 59, Motherboard: 450, Memory: 450, GPU: 450
  - PSU: 450, Case: 450, Storage: 820, Cooler: 450

---

## 5. Compatibility Engine (C#)

- [x] `ICompatibilityEngine` interface + `CompatibilityEngine` service
- [x] `BuildState` DTO — trạng thái build hiện tại từ frontend
- [x] `FilteredResult` + `ComponentDto` — response DTO
- [x] **Pass 1 — Soft filter**: budget (`Price ≤ MaxBudget`), search (ILike), brand
- [x] **Pass 2 — Hard constraints**:
  - CPU ↔ Mainboard: `Socket == SocketCompatibility`
  - RAM ↔ Mainboard: `Type == MemoryCompatibility`
  - GPU ↔ Case: `GPU.Length ≤ Case.MaxVGALength`
  - Mainboard ↔ Case: `FormFactor ∈ FormFactorSupport`
  - PSU ← CPU+GPU: `Wattage ≥ (CPU.TDP + GPU.TDP) × 1.3`
  - Cooler ↔ CPU: socket match + `Cooler.MaxTDP ≥ CPU.TDP`
- [x] **Pass 3 — P/P scoring**: `ApproximatePerformance / Price × 1,000,000`
- [x] Stochastic selection: shuffle top 5 để tránh build lặp lại
- [x] `BuilderController` — `GET /Builder`, `POST /Builder/Filter` (AJAX endpoint)

---

## 6. AI Chatbot (Beginner Builder)

- [x] `IAIAssistantService` + `AIAssistantService`
- [x] **Fallback chain 3 tầng**: Gemini API → Groq API → OpenRouter API
- [x] System prompt chuẩn — parse budget + use-case → JSON params
- [x] `ChatController` — `POST /Chat/Ask`
- [x] Budget allocation theo use-case (gaming: GPU 33%, CPU 24%, v.v.)
- [x] `PickBest()` — chọn component tốt nhất trong allocation budget (±30% flex)
- [x] Floating chat widget (`_ChatWidget.cshtml`) hiện trên mọi trang
- [x] Build card hiển thị build suggestion với nút "Open in PC Builder"
- [x] `sessionStorage` handoff — chuyển AI build sang Builder page, auto preselect

---

## 7. Frontend

- [x] `site.css` — glassmorphism utilities: `.glass`, `.glass-sm`, `.btn-gradient`, `.gradient-text`, `.category-pill`, `.component-card`, `.build-step`, `.perf-bar-*`, `.compat-ok/error`
- [x] `_Layout.cshtml` — dark gradient background, sticky glassmorphism navbar, Bootstrap Icons
- [x] `Views/Home/Index.cshtml` — Hero + 4 feature cards + CTA section
- [x] `Views/Builder/Index.cshtml` — PC Builder UI:
  - Left panel: build steps, stats (price/perf/TDP), recommended PSU, budget filter
  - Right panel: category tabs, search, sort, product grid với AJAX
- [x] `Views/Products/Index.cshtml` — Products page:
  - Filter bar: search, sort, category pills
  - AJAX render grid với `.catch()` error handler
  - Interleaved "all" view (round-robin 3 items/category/page)
- [x] `Views/Account/Login.cshtml`, `Register.cshtml` — glassmorphism + Google button

---
