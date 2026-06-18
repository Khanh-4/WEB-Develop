# 📊 Project Progress

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-18

---

## Overall Progress

```
Setup & Infrastructure   ████████████████████  100%
Database & Migrations    ████████████████████  100%
Authentication           ████████████████████  100%
Data Scraper             ████████████████████  100%
Compatibility Engine     ████████████████████  100%
AI Chatbot               ████████████████████  100%
Frontend (Core pages)    ████████████████████  100%
Cart & Checkout          ░░░░░░░░░░░░░░░░░░░░    0%
Admin Dashboard          ░░░░░░░░░░░░░░░░░░░░    0%
Orders                   ░░░░░░░░░░░░░░░░░░░░    0%
Deployment               ░░░░░░░░░░░░░░░░░░░░    0%
─────────────────────────────────────────────
Overall                  ██████████░░░░░░░░░░   ~55%
```

---

## Data Stats (as of 2026-06-03)

| Bảng | Rows | Nguồn |
|------|------|-------|
| `cpu` | 59 | Phong Vũ |
| `motherboard` | 450 | Phong Vũ |
| `memory` | 450 | Phong Vũ |
| `video_card` | 450 | Phong Vũ |
| `power_supply` | 450 | Phong Vũ |
| `case_enclosure` | 450 | Phong Vũ |
| `storage` | 820 | Phong Vũ (SSD + HDD) |
| `cpu_cooler` | 450 | Phong Vũ |
| **Total** | **3,579** | |

---

## Tech Stack Confirmed

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core MVC (.NET 8) |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | PostgreSQL trên Supabase (Session Pooler) |
| Auth | ASP.NET Core Identity + Google OAuth 2.0 |
| Frontend | Bootstrap 5 + jQuery + Bootstrap Icons + Glassmorphism CSS |
| Data Pipeline | Python 3.11 + BeautifulSoup4 + SQLAlchemy |
| AI | Gemini 1.5 Flash → Groq (llama-3.1-8b) → OpenRouter (fallback chain) |
| Deploy (planned) | Railway |

---

## Key Files

| File | Mô tả |
|------|-------|
| `web/Services/CompatibilityEngine.cs` | Core 3-pass filter engine |
| `web/Services/AIAssistantService.cs` | AI chatbot với 3-tầng fallback |
| `web/Controllers/BuilderController.cs` | AJAX endpoint cho PC Builder |
| `web/Controllers/ChatController.cs` | AI chatbot endpoint |
| `web/Views/Builder/Index.cshtml` | PC Builder UI + AJAX JS |
| `web/Views/Shared/_ChatWidget.cshtml` | Floating chatbot widget |
| `web/wwwroot/css/site.css` | Glassmorphism design system |
| `scraper/scrapers/phongvu.py` | Phong Vũ scraper (9 categories) |
| `scraper/scoring/performance.py` | CPU/GPU heuristic scoring |
| `scraper/processors/normalizer.py` | Unit normalization |

---

## Session 20 — Security & Quality Fixes (2026-06-15)

Code review bằng ECC (7 finder angles × parallel agents) phát hiện và fix 9 bugs:

| Fix | File | Mô tả |
|-----|------|--------|
| QuickQuote form binding | `Index.cshtml:308` | `name="Contact"` → `name="phoneOrEmail"` — form chưa bao giờ hoạt động |
| Recently-viewed XSS | `Index.cshtml:363` | Replace `innerHTML` template literal bằng `createElement`/`textContent` |
| FeaturedCategory XSS | `FeaturedCategory/Default.cshtml:65,123` | `toggleCompare` → `data-cmp-*` attributes (2 nơi) |
| ProductGrid XSS | `_ProductGrid.cshtml:87` | Cùng pattern XSS fix như trên |
| hoursLeft stale | `Index.cshtml:35` | Dùng `ViewData["Now"]` từ controller thay `DateTime.UtcNow` mới |
| Homepage DB error | `HomeController.cs` | `try/catch` around spotlight queries + graceful fallback |
| IMemoryCache | `HomeController.cs` | Cache spotlight 60s → giảm DB load |
| CreatedAt migration | `Cpu`, `VideoCard` | `ALTER TABLE ADD CreatedAt TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP` |
| Cross-table ID fix | `HomeController.cs` | Dùng `CreatedAt` thay `Id` để so sánh "newest" cross-table |

**52/52 Playwright E2E tests pass** sau tất cả fixes.

---

## Session 24 — Check lỗi V2 đóng toàn bộ + fix data CPU (2026-06-18)

**Check lỗi V2 (15 mục) hoàn tất 100%.** Hai PR merged vào `main`:

| PR | Commit | Nội dung |
|----|--------|----------|
| #2 | `2969e3b` | Đợt cuối Check lỗi V2: #3 phần 2 (so sánh build với PC dựng sẵn), #1 (Gmail API gửi mail reset), #4 (seed review), #10/#12/#13 (lọc thương hiệu + dọn data) |
| #3 | `3ecfe0b` | Fix bug xung nhịp CPU sai |

**#3 phần 2 — So sánh với PC dựng sẵn:**
- `GET /Builder/Prebuilts` liệt kê PC dựng sẵn; nút "So sánh với PC dựng sẵn" + picker (DOM API, XSS-safe) → dùng lại `CompareOptions` + `renderMultiResult`. Backend `LabeledBuild.PrebuiltPcId` đã có từ session 22.

**Fix data CPU (BoostClock sai):**
- `parse_clock_ghz()` đọc nhầm field spec → trả giá trị rác (vd 90 GHz) thắng fallback từ tên SP. Hardening: kết quả > 7 GHz trả 0.
- `scraper/fix_cpu_clocks.py` (DRY-RUN mặc định, `--apply` để ghi) — đã sửa **6 dòng** (Base/Boost/ApproximatePerformance tính lại từ tên SP). 0 xung nhịp phi lý còn lại.

Branch `fix/checkv2-ui-batch` + `fix/cpu-clock-data` đã xóa (đã merge).

---

## Next Session Priority

1. **Dọn working tree** — xóa PDF rác trong `TEST_TAY/`, thư mục `C:\Users\...lighthouse.*`, `.pyc` đang track + thêm `.gitignore`
2. **(Tùy chọn) Base clock i9-14900** — id 124/125 đang ước lượng (4.93) vì tên SP thiếu; cần bảng tra base clock nếu muốn hiển thị chính xác
3. **Fix Motherboard socket** — `SocketCompatibility = "Unknown"` làm Compatibility Engine kém chính xác
