# 📊 Project Progress

> Project: **TechSpecs** — E-Commerce + Custom PC Builder
> Last updated: 2026-06-03

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

## Next Session Priority

1. **Cart & Checkout** — quan trọng nhất, web hiện tại chưa bán được hàng
2. **Fix Motherboard socket** — `SocketCompatibility = "Unknown"` làm Compatibility Engine kém chính xác
3. **Admin Dashboard** — cần để quản lý sản phẩm và đơn hàng
