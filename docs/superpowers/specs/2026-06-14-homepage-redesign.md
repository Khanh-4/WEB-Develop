# Homepage Redesign — Design Spec

**Date:** 2026-06-14
**Project:** TechSpecs
**Scope:** Full homepage rebuild — fix light mode, enrich product display, restructure sections

---

## Problem

1. **Light mode invisible text:** `.tx-hero` has hardcoded `color: #fff` and `background: #090d16`. The `[data-theme="light"]` override changes the background to lavender but the `h1` cascade still inherits white text. Structural issue — fragile CSS override chain.

2. **Weak hero:** Carousel shows no real product data. Users see generic copy; no urgency, no price context, no actual products.

3. **Poor section order:** Flash Sale buried at position 6 (high scroll depth). Categories grid below the Quick Actions form. Recently Viewed above Flash Sale.

4. **Product display:** 50 product cards already available via 5 FeaturedCategory ViewComponents — they just needed to be promoted above the fold.

---

## Solution Overview

Three independent changes shipped together:

1. **Hero rewrite** — 2-column static layout (no carousel), right side has 3 live spotlight cards from DB. CSS uses theme variables throughout — no hardcoded colors.
2. **Section reorder** — Flash Sale moves to position 3. Categories up. Featured Products stays central.
3. **Light mode audit** — hero gets clean CSS; other sections verified with `var(--text)` / `var(--text-muted)`.

---

## Section Order (new)

| # | Section | Change |
|---|---------|--------|
| 1 | Hero | **Rebuilt** — 2-col + spotlight cards |
| 2 | Benefit Strip | No change |
| 3 | Flash Sale | **Moved from #6** |
| 4 | Categories Grid | Moved before Quick Actions |
| 5 | Featured Products (CPU→GPU→RAM→MB→Storage) | No change to components |
| 6 | Pre-built PCs | No change |
| 7 | Recently Viewed | No change |
| 8 | Quick Actions + Quote Form | Moved to #8 |
| 9 | Brand Showcase | No change |

---

## Section 1: Hero — Full Spec

### Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  ┌─────────────────────────────┐  ┌──────────────────────────┐  │
│  │  ✦ TECHSPECS               │  │  ⚡ FLASH SALE · 4h còn  │  │
│  │                            │  │  RTX 4070 Ti SUPER 16GB  │  │
│  │  Xây dựng PC Gaming        │  │  18.990.000đ  ~~22.5M~~  │  │
│  │  theo ý tưởng của bạn      │  ├──────────────────────────┤  │
│  │                            │  │  🆕 MỚI VỀ               │  │
│  │  AI tư vấn linh kiện       │  │  Intel Core Ultra 9 285K │  │
│  │  tương thích               │  │  12.500.000đ  CPU        │  │
│  │                            │  ├──────────────────────────┤  │
│  │  [Build PC ngay] [Xem SP]  │  │  🔥 HOT PICK             │  │
│  │                            │  │  MSI B760M Mortar DDR5   │  │
│  └─────────────────────────────┘  │  3.490.000đ  ⭐4.8      │  │
│                                   └──────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

- Mobile: stacks vertically (left col full width, right col hidden on xs, visible from md)
- Right column: 3 stacked cards with gap 10px, no scroll

### CSS Strategy

Replace current hardcoded hero CSS with theme-variable approach:

```css
/* Root-level hero tokens */
:root {
    --hero-bg-start:   #090d16;
    --hero-bg-end:     #0f172a;
    --hero-text:       #ffffff;
    --hero-text-muted: rgba(255,255,255,.75);
    --hero-card-amber-bg:   rgba(245,158,11,.08);
    --hero-card-amber-border: rgba(245,158,11,.3);
    --hero-card-blue-bg:    rgba(56,189,248,.07);
    --hero-card-blue-border: rgba(56,189,248,.25);
    --hero-card-green-bg:   rgba(52,211,153,.07);
    --hero-card-green-border: rgba(52,211,153,.25);
}
[data-theme="light"] {
    --hero-bg-start:   #f0f4ff;
    --hero-bg-end:     #f0f9ff;
    --hero-text:       #0f172a;
    --hero-text-muted: #475569;
    --hero-card-amber-bg:    rgba(245,158,11,.06);
    --hero-card-amber-border: rgba(245,158,11,.25);
    --hero-card-blue-bg:    rgba(37,99,235,.05);
    --hero-card-blue-border: rgba(37,99,235,.2);
    --hero-card-green-bg:   rgba(5,150,105,.05);
    --hero-card-green-border: rgba(5,150,105,.2);
}

.tx-hero {
    background: linear-gradient(135deg, var(--hero-bg-start) 0%, var(--hero-bg-end) 100%);
    color: var(--hero-text);
    /* remove color: #fff — text inherits from here */
}
.tx-hero h1  { color: var(--hero-text); text-shadow: none; }
.tx-hero .lead { color: var(--hero-text-muted); text-shadow: none; }
```

This eliminates the cascade bug entirely — no `[data-theme="light"] .tx-hero h1` override needed.

### Spotlight Cards — Data Sources

3 cards are nullable. If no data → card does not render (slot collapses gracefully).

| Card | Source | Query |
|------|--------|-------|
| ⚡ Flash Sale | `flash_sales` table | `WHERE IsActive=true AND StartsAt<=now AND EndsAt>now ORDER BY EndsAt ASC LIMIT 1` — shows most-expiring-soon item |
| 🆕 Mới về | `cpu` UNION `video_card` | `SELECT Id, 'cpu' as Cat, Name, Price, ImageUrl FROM cpu UNION SELECT Id, 'gpu', Name, Price, ImageUrl FROM video_card ORDER BY Id DESC LIMIT 1` — highest ID = most recently scraped/added |
| 🔥 Hot Pick | `cpu` UNION `video_card` | `WHERE Price > 0 AND ApproximatePerformance > 0 ORDER BY (ApproximatePerformance / Price) DESC LIMIT 1` — best value-for-money ratio |

Each card links to `/Products/Detail/{category}/{id}`.

### ViewModel Changes

```csharp
// web/ViewModels/HomeViewModel.cs
public class HomeViewModel
{
    public List<PrebuiltPcItem> PrebuiltPcs { get; set; } = new();

    // Hero spotlight cards (all nullable — omit card if null)
    public FlashSale? SpotlightFlash { get; set; }
    public SpotlightProductDto? SpotlightNew { get; set; }
    public SpotlightProductDto? SpotlightHot { get; set; }
}

public record SpotlightProductDto(
    int Id,
    string Category,      // "cpu" | "gpu"
    string Name,
    decimal Price,
    string? ImageUrl
);
```

### HomeController Changes

`Index()` becomes `async`, adds 3 queries:

```csharp
public async Task<IActionResult> Index()
{
    var now = DateTime.UtcNow;

    // Spotlight 1: soonest-expiring active flash sale
    var flash = await _db.FlashSales
        .Where(f => f.IsActive && f.StartsAt <= now && f.EndsAt > now)
        .OrderBy(f => f.EndsAt)
        .FirstOrDefaultAsync();

    // Spotlight 2: newest product (highest Id from cpu or gpu)
    var newestCpu = await _db.Cpus
        .OrderByDescending(x => x.Id)
        .Select(x => new SpotlightProductDto(x.Id, "cpu", x.Name, x.Price, x.ImageUrl))
        .FirstOrDefaultAsync();
    var newestGpu = await _db.VideoCards
        .OrderByDescending(x => x.Id)
        .Select(x => new SpotlightProductDto(x.Id, "gpu", x.Name, x.Price, x.ImageUrl))
        .FirstOrDefaultAsync();
    var spotlightNew = (newestCpu, newestGpu) switch {
        (null, var g) => g,
        (var c, null) => c,
        (var c, var g) => c.Id > g.Id ? c : g,
        _ => null
    };

    // Spotlight 3: best value GPU (perf/price ratio). CPU and GPU performance scores
    // use different scales so we don't compare them cross-category — GPU is shown by default
    // since it's the highest-value component decision for most buyers. Fallback to CPU.
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
        PrebuiltPcs      = _mockDataService.GetPrebuiltPcs(),
        SpotlightFlash   = flash,
        SpotlightNew     = spotlightNew,
        SpotlightHot     = spotlightHot,
    };
    return View(vm);
}
```

---

## Section 3: Flash Sale (moved up)

No code change to the ViewComponent. Just moved from position 6 to position 3 in `Index.cshtml`:

```html
<!-- was line 211, now ~line 100 -->
@await Component.InvokeAsync("FlashSale")
```

---

## Light Mode Audit — Non-Hero Sections

| Section | Issue | Fix |
|---------|-------|-----|
| Quick Actions | Build PC button bg: `rgba(139,92,246,.15)` hardcoded | Already semi-transparent purple — acceptable on both themes. Add hover fix for light: `rgba(124,58,237,.12)` |
| Quick Actions | "Xem sản phẩm" btn: `btn-soft` = `rgba(255,255,255,.1)` = invisible on white | Change to `glass-sm` class (already theme-aware) |
| Categories | `.cat-card` — uses `var(--surface-2)` | Already correct ✅ |
| Benefit Strip | `.cat-card` variant — same | Already correct ✅ |
| Featured Products | `FeaturedCategoryViewComponent` — uses `var(--text)` | Already correct ✅ |
| Pre-built PCs | `.pc-body` text uses `var(--text)` | Already correct ✅ |
| Brand Showcase | `style="color:var(--text-muted)"` | Already correct ✅ |

Primary light mode fix is **hero only**. Secondary: `btn-soft` in Quick Actions.

---

## Files Changed

| File | Change |
|------|--------|
| `web/ViewModels/HomeViewModel.cs` | Add `SpotlightFlash`, `SpotlightNew`, `SpotlightHot`, `SpotlightProductDto` |
| `web/Controllers/HomeController.cs` | Make `Index()` async, add 3 spotlight queries |
| `web/Views/Home/Index.cshtml` | Full rewrite — new hero markup, reorder sections |
| `web/wwwroot/css/site.css` | Replace `.tx-hero` color rules with CSS token approach, fix `btn-soft` in Quick Actions |

**Not changed:** All ViewComponents (`FlashSale`, `FeaturedCategory`), product card styles, layout, auth.

---

## Non-Goals

- Adding new product categories to Featured rows (5 existing is sufficient)
- Redesigning the Pre-built PCs card layout
- Changing Quick Actions icon links (just fix `btn-soft` button)
- Admin controls for spotlight cards (system picks automatically)
