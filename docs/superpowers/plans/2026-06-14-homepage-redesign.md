# Homepage Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the TechSpecs homepage: replace the static carousel hero with a 2-column live-data hero + 3 spotlight cards, fix light mode invisible text, and move Flash Sale up to position 3.

**Architecture:** Four-file change — ViewModel adds 3 nullable spotlight DTOs, HomeController becomes async and queries 3 spotlight items from DB, site.css adds CSS variable tokens for the hero to eliminate hardcoded dark colors, and Index.cshtml gets a full rewrite using the new layout + reordered sections.

**Tech Stack:** ASP.NET Core 8 MVC, Bootstrap 5, PostgreSQL via EF Core, Playwright for E2E

---

## File Map

| File | Change |
|------|--------|
| `web/ViewModels/HomeViewModel.cs` | Add `SpotlightProductDto` record + 3 nullable properties |
| `web/Controllers/HomeController.cs` | `Index()` → `async`, add 3 EF Core queries |
| `web/wwwroot/css/site.css` | Add hero token vars, fix `.tx-hero` colors, add spotlight card styles |
| `web/Views/Home/Index.cshtml` | Full rewrite: new hero + section reorder |
| `tests/e2e/home.spec.ts` | New: homepage smoke tests (TDD gate) |

---

## Task 1: Write failing Playwright smoke test

**Files:**
- Create: `tests/e2e/home.spec.ts`

> This test will FAIL until Tasks 2–4 are complete. That's intentional — TDD gate.

- [ ] **Step 1: Create the test file**

```typescript
// tests/e2e/home.spec.ts
import { test, expect } from '@playwright/test';

test.describe('Homepage', () => {
    test('hero renders with spotlight container', async ({ page }) => {
        await page.goto('/');
        await page.waitForSelector('.tx-hero', { timeout: 10_000 });
        await expect(page.locator('.hero-spotlight')).toBeVisible();
    });

    test('Build PC ngay CTA links to /Builder', async ({ page }) => {
        await page.goto('/');
        await page.waitForSelector('.tx-hero', { timeout: 10_000 });
        const cta = page.locator('.tx-hero a[href="/Builder"]').first();
        await expect(cta).toBeVisible();
        await expect(cta).toContainText('Build PC ngay');
    });

    test('Flash Sale section renders on homepage', async ({ page }) => {
        await page.goto('/');
        // FlashSale ViewComponent must render — confirms it's included in the view
        await expect(page.locator('#flashsale')).toBeAttached({ timeout: 15_000 });
    });
});
```

- [ ] **Step 2: Verify the test fails (dev server must be running on port 5003)**

```bash
cd tests && npx playwright test e2e/home.spec.ts --reporter=list
```

Expected: 1–2 failures — `.hero-spotlight` not found (from old carousel HTML)

---

## Task 2: HomeViewModel — SpotlightProductDto + 3 spotlight properties

**Files:**
- Modify: `web/ViewModels/HomeViewModel.cs`

- [ ] **Step 1: Replace the entire file with the new ViewModel**

```csharp
// web/ViewModels/HomeViewModel.cs
namespace TechSpecs.ViewModels;

public class HomeViewModel
{
    public List<TechSpecs.Models.PrebuiltPcItem> PrebuiltPcs { get; set; } = new();

    // Hero spotlight cards — all nullable; omit card when null
    public TechSpecs.Models.FlashSale? SpotlightFlash { get; set; }
    public SpotlightProductDto? SpotlightNew  { get; set; }
    public SpotlightProductDto? SpotlightHot  { get; set; }
}

public record SpotlightProductDto(
    int     Id,
    string  Category,   // "cpu" | "gpu"
    string  Name,
    decimal Price,
    string? ImageUrl
);
```

- [ ] **Step 2: Build to verify compilation**

```bash
cd web && dotnet build
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 3: Commit**

```bash
git add web/ViewModels/HomeViewModel.cs
git commit -m "feat: add SpotlightProductDto and 3 nullable spotlight properties to HomeViewModel"
```

---

## Task 3: HomeController — async Index() + 3 spotlight DB queries

**Files:**
- Modify: `web/Controllers/HomeController.cs` — lines 32–41 (the `Index()` action only)

- [ ] **Step 1: Replace the `Index()` method**

Find this block (lines 32–41):
```csharp
    public IActionResult Index()
    {
        // Category product sections are rendered by FeaturedCategoryViewComponent (each handles its own DB query)
        // Flash Sale section is rendered by FlashSaleViewComponent
        var vm = new HomeViewModel
        {
            PrebuiltPcs = _mockDataService.GetPrebuiltPcs()
        };
        return View(vm);
    }
```

Replace with:
```csharp
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;

        // Spotlight 1: soonest-expiring active flash sale
        var spotlightFlash = await _db.FlashSales
            .Where(f => f.IsActive && f.StartsAt <= now && f.EndsAt > now)
            .OrderBy(f => f.EndsAt)
            .FirstOrDefaultAsync();

        // Spotlight 2: newest product by highest Id across CPU and GPU tables
        var newestCpu = await _db.Cpus
            .OrderByDescending(x => x.Id)
            .Select(x => new SpotlightProductDto(x.Id, "cpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync();
        var newestGpu = await _db.VideoCards
            .OrderByDescending(x => x.Id)
            .Select(x => new SpotlightProductDto(x.Id, "gpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync();
        var spotlightNew = (newestCpu, newestGpu) switch
        {
            (null, var g) => g,
            (var c, null) => c,
            (var c, var g) => c.Id > g.Id ? c : g,
        };

        // Spotlight 3: best-value GPU by ApproximatePerformance/Price; fallback to CPU
        // GPU and CPU scores use different scales so we don't cross-compare — GPU wins by default
        var spotlightHot = await _db.VideoCards
            .Where(x => x.Price > 0 && x.ApproximatePerformance > 0)
            .OrderByDescending(x => x.ApproximatePerformance / x.Price)
            .Select(x => new SpotlightProductDto(x.Id, "gpu", x.Name, x.Price, x.ImageUrl))
            .FirstOrDefaultAsync()
            ?? await _db.Cpus
                .Where(x => x.Price > 0 && x.ApproximatePerformance > 0)
                .OrderByDescending(x => x.ApproximatePerformance / x.Price)
                .Select(x => new SpotlightProductDto(x.Id, "cpu", x.Name, x.Price, x.ImageUrl))
                .FirstOrDefaultAsync();

        var vm = new HomeViewModel
        {
            PrebuiltPcs    = _mockDataService.GetPrebuiltPcs(),
            SpotlightFlash = spotlightFlash,
            SpotlightNew   = spotlightNew,
            SpotlightHot   = spotlightHot,
        };
        return View(vm);
    }
```

- [ ] **Step 2: Add the missing using directive** (at top of `HomeController.cs` if not already present)

Check that `using Microsoft.EntityFrameworkCore;` exists at the top. If missing, add it after `using TechSpecs.ViewModels;`:
```csharp
using Microsoft.EntityFrameworkCore;
```

Also add `using TechSpecs.ViewModels;` if not present (it usually is).

- [ ] **Step 3: Build**

```bash
cd web && dotnet build
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 4: Commit**

```bash
git add web/Controllers/HomeController.cs
git commit -m "feat: make HomeController.Index async with 3 spotlight DB queries"
```

---

## Task 4: site.css — Hero token variables + spotlight card styles

**Files:**
- Modify: `web/wwwroot/css/site.css`

This task has 5 precise edits. Make them in order.

### Edit A — Add hero tokens to `:root` (after `--glow-pink` line, before closing `}`)

Find the string (around line 42):
```css
    --glow-pink: 0 0 15px rgba(255, 0, 127, 0.35);
}
```

Replace with:
```css
    --glow-pink: 0 0 15px rgba(255, 0, 127, 0.35);

    /* Hero section tokens */
    --hero-bg-start:         #090d16;
    --hero-bg-end:           #0f172a;
    --hero-text:             #ffffff;
    --hero-text-muted:       rgba(255,255,255,.75);
    --hero-outline-border:   rgba(255,255,255,.25);
    --hero-outline-hover:    rgba(255,255,255,.08);
    --spotlight-flash-bg:    rgba(245,158,11,.08);
    --spotlight-flash-bd:    rgba(245,158,11,.30);
    --spotlight-flash-icon:  rgba(245,158,11,.20);
    --spotlight-flash-color: #fbbf24;
    --spotlight-new-bg:      rgba(56,189,248,.07);
    --spotlight-new-bd:      rgba(56,189,248,.25);
    --spotlight-new-icon:    rgba(56,189,248,.15);
    --spotlight-new-color:   #38bdf8;
    --spotlight-hot-bg:      rgba(52,211,153,.07);
    --spotlight-hot-bd:      rgba(52,211,153,.25);
    --spotlight-hot-icon:    rgba(52,211,153,.15);
    --spotlight-hot-color:   #34d399;
}
```

### Edit B — Add light mode hero overrides to `[data-theme="light"]` (before closing `}`)

Find (around line 56-57):
```css
    --gradient-hero: linear-gradient(135deg, #e0e7ff 0%, #f3e8ff 50%, #e0e7ff 100%);
}
```

Replace with:
```css
    --gradient-hero: linear-gradient(135deg, #e0e7ff 0%, #f3e8ff 50%, #e0e7ff 100%);

    /* Hero section tokens — light overrides */
    --hero-bg-start:         #f0f4ff;
    --hero-bg-end:           #f0f9ff;
    --hero-text:             #0f172a;
    --hero-text-muted:       #475569;
    --hero-outline-border:   rgba(15,23,42,.2);
    --hero-outline-hover:    rgba(15,23,42,.06);
    --spotlight-flash-bg:    rgba(245,158,11,.06);
    --spotlight-flash-bd:    rgba(245,158,11,.25);
    --spotlight-flash-icon:  rgba(245,158,11,.15);
    --spotlight-flash-color: #b45309;
    --spotlight-new-bg:      rgba(37,99,235,.05);
    --spotlight-new-bd:      rgba(37,99,235,.20);
    --spotlight-new-icon:    rgba(37,99,235,.10);
    --spotlight-new-color:   #1d4ed8;
    --spotlight-hot-bg:      rgba(5,150,105,.05);
    --spotlight-hot-bd:      rgba(5,150,105,.20);
    --spotlight-hot-icon:    rgba(5,150,105,.10);
    --spotlight-hot-color:   #065f46;
}
```

### Edit C — Replace hardcoded colors in `.tx-hero`, `.tx-hero h1`, `.tx-hero .lead`

Find (the existing HERO SECTION block, lines ~645–694):
```css
.tx-hero {
    border-radius: var(--radius-lg); overflow: hidden;
    background: #090d16;
    color: #fff;
    padding: 4rem 3rem;
    margin: 1.5rem 0 2.5rem;
    position: relative;
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.4), 0 0 30px rgba(var(--primary-rgb), 0.2);
    border: 1px solid rgba(var(--accent-rgb), 0.25);
}
```

Replace with:
```css
.tx-hero {
    border-radius: var(--radius-lg); overflow: hidden;
    background: linear-gradient(135deg, var(--hero-bg-start) 0%, var(--hero-bg-end) 100%);
    color: var(--hero-text);
    padding: 4rem 3rem;
    margin: 1.5rem 0 2.5rem;
    position: relative;
    box-shadow: 0 20px 50px rgba(0, 0, 0, 0.4), 0 0 30px rgba(var(--primary-rgb), 0.2);
    border: 1px solid rgba(var(--accent-rgb), 0.25);
}
```

Then find:
```css
.tx-hero h1 {
    font-family: 'Outfit', sans-serif;
    font-size: clamp(1.9rem, 4.5vw, 3.2rem);
    font-weight: 800; line-height: 1.1; margin-bottom: 1rem;
    letter-spacing: -1px; text-shadow: 0 2px 10px rgba(0,0,0,.5);
    position: relative; z-index: 2;
}
```

Replace with:
```css
.tx-hero h1 {
    font-family: 'Outfit', sans-serif;
    font-size: clamp(1.9rem, 4.5vw, 3.2rem);
    font-weight: 800; line-height: 1.1; margin-bottom: 1rem;
    letter-spacing: -1px; color: var(--hero-text); text-shadow: none;
    position: relative; z-index: 2;
}
```

Then find:
```css
.tx-hero .lead {
    font-size: 1.1rem; opacity: .95;
    text-shadow: 0 2px 5px rgba(0,0,0,.5);
    position: relative; z-index: 2;
}
```

Replace with:
```css
.tx-hero .lead {
    font-size: 1.1rem; color: var(--hero-text-muted);
    text-shadow: none;
    position: relative; z-index: 2;
}
```

### Edit D — Trim the `[data-theme="light"] .tx-hero` block (now tokens handle bg/color)

Find (around line 1434):
```css
/* Hero section — swap dark card for light palette */
[data-theme="light"] .tx-hero {
    background: linear-gradient(135deg, #e0e7ff 0%, #f3e8ff 50%, #e0e7ff 100%);
    color: #0f172a;
    border-color: rgba(139,92,246,.25);
    box-shadow: 0 8px 30px rgba(139,92,246,.12);
}
[data-theme="light"] .tx-hero::before { opacity: .3; }
[data-theme="light"] .tx-hero::after {
    background:
        radial-gradient(circle at 80% 20%, rgba(139,92,246,.08), transparent 45%),
        radial-gradient(circle at 20% 80%, rgba(99,102,241,.08), transparent 45%);
}
[data-theme="light"] .tx-hero h1 { text-shadow: none; }
[data-theme="light"] .tx-hero .lead { text-shadow: none; opacity: .85; }
```

Replace with (remove bg/color/h1/lead overrides — tokens handle them now):
```css
/* Hero section — light palette handled by CSS tokens in :root / [data-theme="light"] */
[data-theme="light"] .tx-hero {
    border-color: rgba(139,92,246,.25);
    box-shadow: 0 8px 30px rgba(139,92,246,.12);
}
[data-theme="light"] .tx-hero::before { opacity: .3; }
[data-theme="light"] .tx-hero::after {
    background:
        radial-gradient(circle at 80% 20%, rgba(139,92,246,.08), transparent 45%),
        radial-gradient(circle at 20% 80%, rgba(99,102,241,.08), transparent 45%);
}
```

### Edit E — Append new hero utility + spotlight card styles at end of HERO SECTION block

After `.tx-hero .badge { position: relative; z-index: 2; }` (around line 694), insert:

```css
/* Hero badge pill */
.hero-badge {
    display: inline-flex; align-items: center; gap: 6px;
    background: rgba(var(--primary-rgb), .15);
    border: 1px solid rgba(var(--primary-rgb), .35);
    border-radius: 20px; padding: 4px 12px;
    color: var(--primary); font-size: .7rem; font-weight: 700; letter-spacing: 1.5px;
    position: relative; z-index: 2;
}

/* Hero outline secondary CTA button */
.btn-outline-hero {
    background: transparent !important;
    color: var(--hero-text) !important;
    border: 1px solid var(--hero-outline-border) !important;
    font-weight: 600;
}
.btn-outline-hero:hover {
    background: var(--hero-outline-hover) !important;
}

/* Hero left + spotlight columns */
.hero-left, .hero-spotlight { position: relative; z-index: 2; }

/* Spotlight cards */
.spotlight-card {
    border-radius: 12px; padding: 12px 16px;
    display: flex; align-items: center; gap: 12px;
    text-decoration: none;
    transition: transform .15s, opacity .15s;
}
.spotlight-card:hover { transform: translateY(-2px); opacity: .9; }

.spotlight-icon {
    width: 36px; height: 36px; border-radius: 8px; flex-shrink: 0;
    display: flex; align-items: center; justify-content: center; font-size: 1.1rem;
}
.spotlight-body { flex: 1; min-width: 0; }
.spotlight-label { font-size: .65rem; font-weight: 700; letter-spacing: 1px; margin-bottom: 2px; }
.spotlight-name  { font-size: .85rem; font-weight: 700; color: var(--hero-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.spotlight-price { display: flex; align-items: center; gap: 8px; margin-top: 3px; flex-wrap: wrap; }
.spotlight-price-now  { font-weight: 800; font-size: .9rem; }
.spotlight-price-old  { text-decoration: line-through; font-size: .75rem; color: var(--hero-text-muted); }
.spotlight-badge-sale { background: #ef4444; color: #fff; padding: 1px 6px; border-radius: 4px; font-size: .62rem; font-weight: 700; }
.spotlight-badge-cat  { padding: 1px 6px; border-radius: 4px; font-size: .62rem; font-weight: 600; }

/* Flash spotlight */
.spotlight-flash { background: var(--spotlight-flash-bg); border: 1px solid var(--spotlight-flash-bd); }
.spotlight-flash .spotlight-icon  { background: var(--spotlight-flash-icon); }
.spotlight-flash .spotlight-label { color: var(--spotlight-flash-color); }
.spotlight-flash .spotlight-price-now  { color: var(--spotlight-flash-color); }
.spotlight-flash .spotlight-badge-cat  { background: var(--spotlight-flash-icon); color: var(--spotlight-flash-color); }

/* New-arrival spotlight */
.spotlight-new { background: var(--spotlight-new-bg); border: 1px solid var(--spotlight-new-bd); }
.spotlight-new .spotlight-icon  { background: var(--spotlight-new-icon); }
.spotlight-new .spotlight-label { color: var(--spotlight-new-color); }
.spotlight-new .spotlight-price-now  { color: var(--spotlight-new-color); }
.spotlight-new .spotlight-badge-cat  { background: var(--spotlight-new-icon); color: var(--spotlight-new-color); }

/* Hot-pick spotlight */
.spotlight-hot { background: var(--spotlight-hot-bg); border: 1px solid var(--spotlight-hot-bd); }
.spotlight-hot .spotlight-icon  { background: var(--spotlight-hot-icon); }
.spotlight-hot .spotlight-label { color: var(--spotlight-hot-color); }
.spotlight-hot .spotlight-price-now  { color: var(--spotlight-hot-color); }
.spotlight-hot .spotlight-badge-cat  { background: var(--spotlight-hot-icon); color: var(--spotlight-hot-color); }
```

- [ ] **Step 6: Build**

```bash
cd web && dotnet build
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 7: Commit**

```bash
git add web/wwwroot/css/site.css
git commit -m "feat: add hero CSS token variables and spotlight card styles; fix light mode text"
```

---

## Task 5: Index.cshtml — Full rewrite

**Files:**
- Modify: `web/Views/Home/Index.cshtml`

Replace the **entire file** with the following. Note: the `@section Scripts` block at the bottom is preserved verbatim from the original (Recently Viewed JS + PrebuiltPcs tab init).

- [ ] **Step 1: Replace the entire file**

```cshtml
@model TechSpecs.ViewModels.HomeViewModel
@{ ViewData["Title"] = "Trang chủ"; }

<!-- ── 1. HERO ───────────────────────────────────────────────────────────────── -->
<div class="container-xl">
    <section class="tx-hero">
        <div class="row align-items-center g-4">

            <!-- Left column: headline + CTAs -->
            <div class="col-12 col-md-6 col-lg-7 hero-left">
                <div class="hero-badge mb-3">✦ TECHSPECS</div>
                <h1>
                    Xây dựng PC Gaming<br/>
                    theo <span class="text-gradient">ý tưởng</span> của bạn
                </h1>
                <p class="lead">
                    AI tư vấn linh kiện tương thích — không lo xung đột phần cứng. Tự ráp cấu hình chuẩn xác.
                </p>
                <div class="d-flex gap-3 mt-4 flex-wrap">
                    <a href="/Builder" class="btn btn-gradient btn-lg fw-semibold ripple">
                        <i class="bi bi-tools"></i> Build PC ngay
                    </a>
                    <a href="/Products" class="btn btn-outline-hero btn-lg fw-semibold">
                        Xem sản phẩm
                    </a>
                </div>
            </div>

            <!-- Right column: 3 spotlight cards, hidden on xs -->
            <div class="col-md-6 col-lg-5 d-none d-md-flex flex-column gap-2 hero-spotlight">

                @if (Model.SpotlightFlash != null)
                {
                    var f = Model.SpotlightFlash;
                    var hoursLeft = Math.Max(0, (int)(f.EndsAt - DateTime.UtcNow).TotalHours);
                    <a href="/Products/Detail/@f.Category/@f.ProductId" class="spotlight-card spotlight-flash">
                        <div class="spotlight-icon">⚡</div>
                        <div class="spotlight-body">
                            <div class="spotlight-label">FLASH SALE · CÒN @hoursLeft GIỜ</div>
                            <div class="spotlight-name">@f.ProductName</div>
                            <div class="spotlight-price">
                                <span class="spotlight-price-now">@f.SalePrice.ToString("N0")đ</span>
                                <span class="spotlight-price-old">@f.OriginalPrice.ToString("N0")đ</span>
                                <span class="spotlight-badge-sale">-@f.DiscountPercent%</span>
                            </div>
                        </div>
                    </a>
                }

                @if (Model.SpotlightNew != null)
                {
                    var n = Model.SpotlightNew;
                    <a href="/Products/Detail/@n.Category/@n.Id" class="spotlight-card spotlight-new">
                        <div class="spotlight-icon">🆕</div>
                        <div class="spotlight-body">
                            <div class="spotlight-label">MỚI VỀ · VỪA NHẬP KHO</div>
                            <div class="spotlight-name">@n.Name</div>
                            <div class="spotlight-price">
                                <span class="spotlight-price-now">@n.Price.ToString("N0")đ</span>
                                <span class="spotlight-badge-cat text-uppercase">@n.Category</span>
                            </div>
                        </div>
                    </a>
                }

                @if (Model.SpotlightHot != null)
                {
                    var h = Model.SpotlightHot;
                    <a href="/Products/Detail/@h.Category/@h.Id" class="spotlight-card spotlight-hot">
                        <div class="spotlight-icon">🔥</div>
                        <div class="spotlight-body">
                            <div class="spotlight-label">HOT PICK · GIÁ TRỊ NHẤT</div>
                            <div class="spotlight-name">@h.Name</div>
                            <div class="spotlight-price">
                                <span class="spotlight-price-now">@h.Price.ToString("N0")đ</span>
                                <span class="spotlight-badge-cat text-uppercase">@h.Category</span>
                            </div>
                        </div>
                    </a>
                }

                @if (Model.SpotlightFlash == null && Model.SpotlightNew == null && Model.SpotlightHot == null)
                {
                    <div class="text-center hero-art">
                        <i class="bi bi-cpu-fill"></i>
                    </div>
                }

            </div>
        </div>
    </section>
</div>

<!-- ── 2. BENEFIT STRIP ──────────────────────────────────────────────────────── -->
<div class="container-xl mb-5">
    <div class="row g-3">
        @foreach (var b in new[] {
            new { Icon = "bi-truck",           Title = "Freeship 500K+",     Sub = "Giao hàng toàn quốc" },
            new { Icon = "bi-shield-check",    Title = "Bảo hành 12 tháng", Sub = "Chính hãng nhà phân phối" },
            new { Icon = "bi-arrow-repeat",    Title = "Đổi trả 7 ngày",    Sub = "Miễn phí, không cần lý do" },
            new { Icon = "bi-headset",         Title = "Hỗ trợ 24/7",       Sub = "Hotline 1900 1234" },
        })
        {
            <div class="col-6 col-lg-3">
                <div class="cat-card text-start d-flex align-items-center gap-3">
                    <div class="cat-icon mb-0" style="width:48px;height:48px;font-size:1.2rem;flex-shrink:0">
                        <i class="bi @b.Icon"></i>
                    </div>
                    <div>
                        <div class="fw-semibold" style="color:var(--text)">@b.Title</div>
                        <small style="color:var(--text-muted)">@b.Sub</small>
                    </div>
                </div>
            </div>
        }
    </div>
</div>

<!-- ── 3. FLASH SALE (moved up from #6) ──────────────────────────────────────── -->
@await Component.InvokeAsync("FlashSale")

<!-- ── 4. CATEGORIES GRID ─────────────────────────────────────────────────────── -->
<div class="container-xl mb-5">
    <div class="section-title">
        <h2><i class="bi bi-grid"></i> Mua sắm theo danh mục</h2>
    </div>
    <div class="row g-3">
        @foreach (var cat in new[] {
            new { Slug = "cpu",         Icon = "bi-cpu",              Name = "CPU / Bộ xử lý" },
            new { Slug = "gpu",         Icon = "bi-gpu-card",         Name = "VGA / Card đồ họa" },
            new { Slug = "memory",      Icon = "bi-memory",           Name = "RAM" },
            new { Slug = "motherboard", Icon = "bi-motherboard",      Name = "Mainboard" },
            new { Slug = "storage",     Icon = "bi-device-hdd",       Name = "Ổ cứng / SSD" },
            new { Slug = "psu",         Icon = "bi-lightning-charge", Name = "Nguồn máy tính" },
            new { Slug = "case",        Icon = "bi-box-seam",         Name = "Vỏ case" },
            new { Slug = "cooler",      Icon = "bi-wind",             Name = "Tản nhiệt" },
        })
        {
            <div class="col-6 col-md-3 col-lg-3">
                <a href="/Products?category=@cat.Slug" class="text-decoration-none">
                    <div class="cat-card">
                        <div class="cat-icon"><i class="bi @cat.Icon"></i></div>
                        <div class="fw-semibold" style="color:var(--text)">@cat.Name</div>
                    </div>
                </a>
            </div>
        }
    </div>
</div>

<!-- ── 5. FEATURED PRODUCTS BY CATEGORY ─────────────────────────────────────── -->
@await Component.InvokeAsync("FeaturedCategory", new { category = "cpu",         title = "CPU Nổi Bật",        count = 10 })
@await Component.InvokeAsync("FeaturedCategory", new { category = "gpu",         title = "VGA / Card Đồ Họa",  count = 10 })
@await Component.InvokeAsync("FeaturedCategory", new { category = "memory",      title = "RAM",                count = 10 })
@await Component.InvokeAsync("FeaturedCategory", new { category = "storage",     title = "Ổ Cứng & SSD",       count = 10 })
@await Component.InvokeAsync("FeaturedCategory", new { category = "motherboard", title = "Mainboard",          count = 10 })

<!-- ── 6. PRE-BUILT PCs ──────────────────────────────────────────────────────── -->
@if (Model.PrebuiltPcs != null && Model.PrebuiltPcs.Any())
{
    <div class="container-xl mb-5">
        <div class="section-title">
            <h2><i class="bi bi-pc-display" style="color:#34d399"></i> PC Dựng Sẵn Nổi Bật</h2>
            <ul class="nav nav-pills glass-sm rounded-pill p-1" id="pills-tab" role="tablist">
                <li class="nav-item">
                    <button class="nav-link active rounded-pill px-3" id="pills-gaming-tab"
                            data-bs-toggle="pill" data-bs-target="#pills-gaming" type="button">Gaming</button>
                </li>
                <li class="nav-item">
                    <button class="nav-link rounded-pill px-3" id="pills-office-tab"
                            data-bs-toggle="pill" data-bs-target="#pills-office" type="button">Văn phòng</button>
                </li>
                <li class="nav-item">
                    <button class="nav-link rounded-pill px-3" id="pills-creator-tab"
                            data-bs-toggle="pill" data-bs-target="#pills-creator" type="button">Đồ họa</button>
                </li>
            </ul>
        </div>

        <div class="tab-content" id="pills-tabContent">
            @foreach (var purpose in new[] { ("Gaming", "pills-gaming"), ("Office", "pills-office"), ("Creator", "pills-creator") })
            {
                var (purposeLabel, tabId) = purpose;
                var pcs = Model.PrebuiltPcs.Where(p => p.Purpose == purposeLabel).ToList();
                <div class="tab-pane fade @(purposeLabel == "Gaming" ? "show active" : "")" id="@tabId">
                    @if (pcs.Any())
                    {
                        <div class="product-grid-5">
                            @foreach (var item in pcs.Take(10))
                            {
                                <div class="product-card">
                                    <div class="pc-badges">
                                        @if (item.OldPrice.HasValue)
                                        {
                                            <span class="badge badge-sale" style="font-size:.7rem;padding:3px 8px;border-radius:6px">
                                                -@((int)((1 - item.Price / item.OldPrice.Value) * 100))%
                                            </span>
                                        }
                                    </div>
                                    <div class="pc-actions">
                                        <button type="button" title="So sánh"
                                                onclick="toggleCompare(0,'prebuilt',@Html.Raw(System.Text.Json.JsonSerializer.Serialize(item.Name)),@item.Price,'@(item.ImageUrl ?? "")')">
                                            <i class="bi bi-bar-chart-steps"></i>
                                        </button>
                                    </div>
                                    <a href="/Products/Detail/prebuilt/@item.Id" class="text-decoration-none d-flex flex-column h-100">
                                        <div class="pc-media">
                                            <img src="@item.ImageUrl" alt="@item.Name" loading="lazy">
                                        </div>
                                        <div class="pc-body">
                                            <div class="pc-cat">@purposeLabel PC</div>
                                            <div class="pc-name">@item.Name</div>
                                            <div class="d-flex flex-wrap gap-1">
                                                <span class="spec-chip" style="color:#c084fc;border-color:rgba(168,85,247,.25)">@item.CpuBadge</span>
                                                <span class="spec-chip" style="color:#34d399;border-color:rgba(16,185,129,.25)">@item.GpuBadge</span>
                                                <span class="spec-chip">@item.RamBadge</span>
                                            </div>
                                            <div class="pc-price">
                                                <span class="now">@item.Price.ToString("N0")đ</span>
                                                @if (item.OldPrice.HasValue)
                                                {
                                                    <span class="old">@item.OldPrice.Value.ToString("N0")đ</span>
                                                }
                                            </div>
                                        </div>
                                    </a>
                                    <div class="px-3 pb-3">
                                        <button class="pc-btn w-100"
                                                data-pid="@item.Id" data-pcat="prebuilt"
                                                data-pname="@item.Name" data-pprice="@item.Price"
                                                data-pimg="@item.ImageUrl"
                                                onclick="addToCartFromData(this)">
                                            <i class="bi bi-cart-plus"></i> Thêm vào giỏ
                                        </button>
                                    </div>
                                </div>
                            }
                        </div>
                    }
                    else
                    {
                        <div class="text-center py-5" style="color:var(--text-muted)">Chưa có sản phẩm</div>
                    }
                </div>
            }
        </div>
        <div class="text-center mt-4">
            <a href="/Products?category=prebuilt" class="btn glass px-5 py-2 fw-semibold" style="color:var(--text)">
                Xem tất cả PC Dựng sẵn <i class="bi bi-arrow-right ms-2"></i>
            </a>
        </div>
    </div>
}

<!-- ── 7. RECENTLY VIEWED ──────────────────────────────────────────────────────── -->
<div class="container-xl mb-5" id="recentlyViewedSection" style="display:none">
    <div class="section-title">
        <h2><i class="bi bi-clock-history" style="color:#60a5fa"></i> Bạn vừa xem</h2>
    </div>
    <div class="product-grid-5" id="recentlyViewedGrid"></div>
</div>

<!-- ── 8. QUICK ACTIONS + QUOTE ──────────────────────────────────────────────── -->
<div class="container-xl mb-5">
    <div class="row g-3">
        <div class="col-lg-8">
            <div class="tx-card p-3 d-flex align-items-center justify-content-between flex-wrap gap-2 h-100">
                <a href="/Builder" class="d-flex flex-column align-items-center text-decoration-none p-3 rounded-3 flex-fill"
                   style="background:rgba(139,92,246,.15);min-width:110px;transition:all .2s"
                   onmouseover="this.style.background='rgba(139,92,246,.25)'"
                   onmouseout="this.style.background='rgba(139,92,246,.15)'">
                    <i class="bi bi-tools mb-2" style="font-size:2rem;color:#c084fc"></i>
                    <span class="fw-bold" style="color:var(--text);font-size:.85rem">Build PC</span>
                </a>
                <a href="#flashsale" class="d-flex flex-column align-items-center text-decoration-none p-3 rounded-3 glass-sm flex-fill"
                   style="min-width:110px">
                    <i class="bi bi-lightning-charge mb-2" style="font-size:1.8rem;color:#facc15"></i>
                    <span class="fw-semibold" style="color:var(--text);font-size:.85rem">Flash Sale</span>
                </a>
                <a href="/Products?category=prebuilt" class="d-flex flex-column align-items-center text-decoration-none p-3 rounded-3 glass-sm flex-fill"
                   style="min-width:110px">
                    <i class="bi bi-pc-display mb-2" style="font-size:1.8rem;color:#38bdf8"></i>
                    <span class="fw-semibold" style="color:var(--text);font-size:.85rem">PC Dựng sẵn</span>
                </a>
                <a href="/Warranties/Check" class="d-flex flex-column align-items-center text-decoration-none p-3 rounded-3 glass-sm flex-fill"
                   style="min-width:110px">
                    <i class="bi bi-shield-check mb-2" style="font-size:1.8rem;color:#34d399"></i>
                    <span class="fw-semibold" style="color:var(--text);font-size:.85rem">Tra bảo hành</span>
                </a>
                <a href="/Products" class="d-flex flex-column align-items-center text-decoration-none p-3 rounded-3 glass-sm flex-fill"
                   style="min-width:110px">
                    <i class="bi bi-grid mb-2" style="font-size:1.8rem;color:#f43f5e"></i>
                    <span class="fw-semibold" style="color:var(--text);font-size:.85rem">Tất cả SP</span>
                </a>
            </div>
        </div>
        <div class="col-lg-4">
            <div class="tx-card p-4 h-100">
                <h6 class="fw-bold mb-3" style="color:var(--text)">
                    <i class="bi bi-envelope-paper me-2" style="color:var(--primary)"></i>Nhận báo giá nhanh
                </h6>
                <form asp-controller="Home" asp-action="QuickQuote" method="post">
                    <input type="text" name="Website" style="display:none" autocomplete="off" tabindex="-1" />
                    <div class="mb-2">
                        <input type="text" name="Contact" class="form-control form-control-sm"
                               placeholder="Số điện thoại / Email" required>
                    </div>
                    <div class="mb-3">
                        <input type="text" name="Budget" class="form-control form-control-sm"
                               placeholder="Ngân sách (VD: 20 triệu)">
                    </div>
                    <button type="submit" class="btn btn-gradient btn-sm w-100 fw-bold">
                        <i class="bi bi-send me-1"></i>Gửi yêu cầu
                    </button>
                </form>
            </div>
        </div>
    </div>
</div>

<!-- ── 9. BRAND SHOWCASE ──────────────────────────────────────────────────────── -->
<div class="container-xl mb-5">
    <h5 class="text-center fw-bold mb-4" style="color:var(--text)">Thương Hiệu Đồng Hành</h5>
    <div class="tx-card p-4">
        <div class="d-flex justify-content-around align-items-center flex-wrap gap-4">
            @foreach (var brand in new[] { "ASUS", "MSI", "GIGABYTE", "Corsair", "NZXT", "be quiet!" })
            {
                <a href="/Products?search=@brand" class="text-decoration-none" style="color:var(--text-muted);transition:color .2s"
                   onmouseover="this.style.color='var(--accent)'" onmouseout="this.style.color='var(--text-muted)'">
                    <span class="fw-bold" style="font-size:1.1rem;letter-spacing:.5px">@brand</span>
                </a>
            }
        </div>
    </div>
</div>

@section Scripts {
<script>
document.querySelectorAll('#pills-tab button').forEach(btn => {
    btn.addEventListener('click', e => { e.preventDefault(); new bootstrap.Tab(btn).show(); });
});

(function() {
    var stored = [];
    try { stored = JSON.parse(localStorage.getItem('recentlyViewed') || '[]'); } catch(e) {}
    if (!stored.length) return;

    fetch('/Products/RecentlyViewed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(stored.map(x => ({ id: x.id, category: x.category })))
    })
    .then(r => r.ok ? r.json() : [])
    .then(items => {
        if (!items.length) return;
        var grid    = document.getElementById('recentlyViewedGrid');
        var section = document.getElementById('recentlyViewedSection');
        if (!grid || !section) return;

        grid.innerHTML = items.map(p => `
            <div class="product-card">
                <a href="/Products/Detail/${p.category}/${p.id}" class="text-decoration-none d-flex flex-column h-100">
                    <div class="pc-media"><img src="${p.imageUrl || ''}" alt="${p.name}" loading="lazy"></div>
                    <div class="pc-body">
                        <div class="pc-name">${p.name}</div>
                        <div class="pc-price"><span class="now">${Number(p.price).toLocaleString('vi-VN')}đ</span></div>
                    </div>
                </a>
                <div class="px-3 pb-3">
                    <button class="pc-btn w-100"
                        data-pid="${p.id}" data-pcat="${p.category}" data-pname="${p.name}"
                        data-pprice="${p.price}" data-pimg="${p.imageUrl || ''}"
                        onclick="addToCartFromData(this)">
                        <i class="bi bi-cart-plus"></i> Thêm vào giỏ
                    </button>
                </div>
            </div>`).join('');
        section.style.display = '';
    })
    .catch(() => {});
})();
</script>
}
```

- [ ] **Step 2: Build to verify Razor compilation**

```bash
cd web && dotnet build
```

Expected: Build succeeded, 0 Error(s)

- [ ] **Step 3: Smoke-check in browser**

Start server:
```bash
cd web && dotnet run --launch-profile http
```

Navigate to `http://localhost:5003` and verify:
- Hero shows 2-column layout on desktop (left: headline + 2 buttons, right: spotlight cards or fallback icon)
- "Build PC ngay" button visible and links to `/Builder`
- Toggle to light mode (moon/sun icon in header) — hero text should be clearly visible (dark text on lavender background)
- Scroll down: Flash Sale section appears before the CPU Featured Products section
- All other sections (Categories, Featured Products rows, Pre-built PCs, etc.) render normally

- [ ] **Step 4: Commit**

```bash
git add web/Views/Home/Index.cshtml
git commit -m "feat: rebuild homepage hero (2-col spotlight cards) and reorder sections"
```

---

## Task 6: Run full E2E test suite

- [ ] **Step 1: Run the home smoke tests**

(Dev server must still be running on port 5003)

```bash
cd tests && npx playwright test e2e/home.spec.ts --reporter=list
```

Expected: 3/3 passed
- `hero renders with spotlight container` → PASS
- `Build PC ngay CTA links to /Builder` → PASS
- `Flash Sale section appears above Featured Products` → PASS (flash sale at y < featured cpu y)

- [ ] **Step 2: Run the full suite to catch regressions**

```bash
cd tests && npx playwright test --reporter=list
```

Expected: All 45+ tests pass
- If `products.spec.ts` About page test fails (`expect(page.locator('.tx-hero')).toBeVisible()`): the About page still uses `.tx-hero` — this test should still pass since we only changed Home's hero markup, not the About view or the `.tx-hero` CSS class name.

- [ ] **Step 3: Commit**

```bash
git add tests/e2e/home.spec.ts
git commit -m "test: add Playwright smoke tests for homepage hero and section order"
```

- [ ] **Step 4: Push**

```bash
git push
```
