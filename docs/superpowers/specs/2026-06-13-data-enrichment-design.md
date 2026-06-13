# Data Enrichment — Spec & Review Pipeline

**Date:** 2026-06-13  
**Project:** TechSpecs  
**Scope:** Fix missing specs on all 8 hardware categories + scrape full review articles from Tinhte/GenK/Tom's Hardware and match to products

---

## Problem

1. **Missing specs**: Many products in DB have NULL fields critical for Builder compatibility (TDP, socket, RAM type, chipset, GPU length, form factor). Root cause: scrapers read listing pages; detail pages have complete spec tables but are not fully parsed.

2. **No editorial content**: Product Detail page has user reviews but no professional tech articles/analysis — essential for a credible tech site. Users have no way to read in-depth coverage.

---

## Architecture

Two independent workstreams running in parallel:

```
Workstream 1 — Spec Enrichment
  enricher.py
    └─ for each product with NULL fields in DB
         → re-fetch product detail page (URL stored per product)
         → parse spec table HTML
         → UPDATE only NULL fields (never overwrite good data)

Workstream 2 — Review Scraper
  scrapers/reviews/
    ├─ base_review.py
    ├─ tinhte.py
    ├─ genk.py
    └─ tomshardware.py
         → generate seed keywords from top products in DB
         → search each site for those keywords
         → scrape full article HTML + clean
         → fuzzy match title → product in DB
         → INSERT into product_articles
```

**CLI entry points:**
```bash
python enricher.py                              # fill missing specs
python review_scraper.py                        # scrape all 3 sources
python review_scraper.py --source tinhte,genk  # selective
```

**GitHub Actions:** add 2 new cron jobs (weekly cadence — review content changes slowly).

---

## Workstream 1 — Spec Enrichment

### DB change

Add `source_url TEXT` column to all 8 hardware tables if not already present. This stores the product detail page URL from scrape time (needed to re-fetch).

Migration: `AddSourceUrlToHardwareTables`

### enricher.py logic

```
for each category in [cpu, gpu, ram, motherboard, psu, case, storage, cooler]:
    products = SELECT * FROM {category} WHERE any critical field IS NULL
    for each product:
        html = fetch(product.source_url, delay=0.3s)
        specs = parse_spec_table(html)          # extract <table> or spec div
        patch = {k: v for k, v in specs if current[k] is None}
        if patch:
            UPDATE {category} SET ... WHERE id = product.id
```

### Fields to fill per category

| Category | Critical NULL fields |
|----------|---------------------|
| CPU | CoreCount, BoostClock, TDP, Socket, Cache |
| GPU | VRAM, Length (mm), TDP |
| RAM | Type (DDR4/DDR5), Speed (MHz) |
| Motherboard | Socket, ChipSet, FormFactor, RAMType, RAMSlots, MaxRAM |
| PSU | Wattage, EfficiencyRating |
| Case | FormFactorSupport, MaxVGALength, MaxCoolerHeight |
| Storage | Interface (NVMe/SATA), ReadSpeed, WriteSpeed |
| Cooler | SocketList, MaxTDP, Height |

### Spec table parsing

Each source site (PhongVu, AnPhat, TTGShop, GearVN) has a slightly different spec table structure. Each existing scraper file gets a `parse_detail_specs(html) -> dict` function added. `enricher.py` calls the correct parser based on `source_url` domain.

---

## Workstream 2 — Review Scraper

### New DB table: `product_articles`

```sql
CREATE TABLE product_articles (
    id               SERIAL PRIMARY KEY,
    product_category VARCHAR(20),       -- 'gpu', 'cpu', 'ram', etc.
    product_id       INT,               -- FK to matched product (nullable if unmatched)
    source           VARCHAR(20),       -- 'tinhte', 'genk', 'tomshardware'
    url              TEXT UNIQUE,       -- prevents re-scraping same article
    title            TEXT NOT NULL,
    content          TEXT,              -- cleaned article HTML
    thumbnail_url    TEXT,
    published_at     TIMESTAMPTZ,
    scraped_at       TIMESTAMPTZ DEFAULT NOW(),
    match_score      FLOAT              -- 0.0–1.0 for audit/debug
);
```

EF Core: add `DbSet<ProductArticle>` to `AppDbContext`, EF migration `AddProductArticles`.

### Seed keyword generation

```python
# For each category, take top 50 products by ApproximatePerformance
# Extract short model keyword from full product name:
#   "ASUS GeForce RTX 4070 Ti Super ROG Strix OC" → "RTX 4070 Ti Super"
# Regex patterns per category:
#   GPU:     r'(RTX|RX|GTX|Arc A)\s[\d\w\s]+'
#   CPU:     r'(Core i[3579][-\s]\d+\w*|Ryzen [3579]\s\d+\w*)'
#   RAM:     r'(DDR[45][-\s]\d{3,4})'
#   Storage: r'(\d+\s?[GT]B\s+(?:NVMe|SSD|M\.2))'
# Limit: max 3 articles per product per source (avoid flooding)
```

### Site-specific strategies

| Site | Seed URL pattern | Article selector | Date selector |
|------|-----------------|-----------------|---------------|
| Tinhte.vn | `/search.php?q={keyword}` → filter type=article | `div.article-content` | `time[datetime]` |
| GenK.vn | `/tag/{keyword-slug}.chn` | `div.detail-content` | `span.time-ago-last-edited` |
| Tom's Hardware | `/search/?q={keyword}&type=review` | `div#article-body` | `time[datetime]` |

Request delay: 1.0s between requests (more conservative than product scraper — review sites have stricter rate limits).

### Fuzzy match logic

```python
from rapidfuzz import process, fuzz

def match_article_to_product(title: str, category: str, db) -> tuple[int|None, float]:
    # Step 1: extract model keyword from article title
    keyword = extract_model_keyword(title, category)  # regex per category
    if not keyword:
        return None, 0.0

    # Step 2: DB candidates via ILIKE — use first 2 tokens for precision
    # "RTX 4070 Ti Super" → anchor "%RTX 4070%" not just "%RTX%" (too broad)
    anchor = " ".join(keyword.split()[:2])
    candidates = db.query(
        f"SELECT id, name FROM {category} WHERE name ILIKE %s LIMIT 20",
        f"%{anchor}%"
    )
    if not candidates:
        return None, 0.0

    # Step 3: rapidfuzz score
    names = {str(p.id): p.name for p in candidates}
    result = process.extractOne(keyword, names, scorer=fuzz.token_sort_ratio)
    if result and result[1] >= 75:
        return int(result[2]), result[1] / 100
    return None, 0.0
```

**Threshold 75** chosen to avoid false positives like "RTX 4070" matching "RTX 4070 Ti". Articles with `product_id = NULL` are stored but not shown on frontend.

### Content cleaning

Use `beautifulsoup4` (already a dependency). Strip: `<script>`, `<style>`, ads, social widgets, comment sections. Keep: `<p>`, `<h2>`, `<h3>`, `<img>`, `<ul>`, `<ol>`, `<table>`, `<strong>`, `<em>`. Rewrite all `<img src>` to absolute URLs.

```python
def clean_article_html(raw_html: str, base_url: str) -> str:
    soup = BeautifulSoup(raw_html, "html.parser")
    for tag in soup(["script", "style", "iframe", "aside"]):
        tag.decompose()
    for img in soup.find_all("img"):
        img["src"] = urljoin(base_url, img.get("src", ""))
        img["loading"] = "lazy"
    return str(soup)
```

---

## Frontend — Product Detail "Bài đánh giá" tab

### New endpoint

`GET /Products/Articles?category=gpu&id=42`  
Returns `List<ProductArticleDto>` ordered by `published_at DESC`, max 10.

### UI layout

New tab added to the existing tab row on `Views/Products/Detail.cshtml`:

```
[Thông số kỹ thuật]  [Đánh giá & Hỏi đáp]  [Bài đánh giá]
```

Article list inside tab:

```
┌──────────────────────────────────────────────────────────────┐
│  [Tinhte]  RTX 4070 Ti Super: Lựa chọn hoàn hảo cho 2024   │
│  Tinhte.vn · 12/03/2024                                      │
│  Dòng đầu bài viết làm excerpt (truncate 200 ký tự)...      │
│                                        [Đọc bài đầy đủ ↓]   │
├──────────────────────────────────────────────────────────────┤
│  [GenK]  So sánh RTX 4070 Ti Super vs RX 7900 XTX...        │
│  ...                                                         │
└──────────────────────────────────────────────────────────────┘
```

"Đọc bài đầy đủ" → Bootstrap accordion expand, render `content` HTML inline. No redirect — keeps user on site.

Source badges: colored pill per source (`Tinhte` = blue, `GenK` = orange, `Tom's Hardware` = red).

Empty state: "Chưa có bài đánh giá cho sản phẩm này." (hidden tab if 0 articles).

---

## File Changes Summary

### Scraper (`/scraper`)

| File | Change |
|------|--------|
| `enricher.py` | New — spec fill-in runner |
| `scrapers/phongvu.py` | Add `parse_detail_specs(html)` |
| `scrapers/anphat.py` | Add `parse_detail_specs(html)` |
| `scrapers/ttgshop.py` | Add `parse_detail_specs(html)` |
| `scrapers/gearvn.py` | Add `parse_detail_specs(html)` |
| `scrapers/reviews/base_review.py` | New — abstract base class |
| `scrapers/reviews/tinhte.py` | New |
| `scrapers/reviews/genk.py` | New |
| `scrapers/reviews/tomshardware.py` | New |
| `review_scraper.py` | New — review pipeline runner |
| `requirements.txt` | Add `rapidfuzz`, `html-sanitizer` |

### Web (`/web`)

| File | Change |
|------|--------|
| `Models/ProductArticle.cs` | New model |
| `Data/AppDbContext.cs` | Add `DbSet<ProductArticle>` |
| `Data/Migrations/` | `AddProductArticles` + `AddSourceUrlToHardwareTables` |
| `Controllers/ProductsController.cs` | Add `GET /Products/Articles` endpoint |
| `Views/Products/Detail.cshtml` | Add "Bài đánh giá" tab + article list UI |
| `.github/workflows/scraper.yml` | Add weekly jobs for enricher + review_scraper |

---

## Build sequence

1. DB migrations (source_url columns + product_articles table)
2. `enricher.py` + `parse_detail_specs()` per scraper source
3. Review scraper base class + fuzzy match util
4. Tinhte → GenK → Tom's Hardware scrapers (in order of complexity)
5. Web: ProductArticle model + endpoint + frontend tab
6. GitHub Actions cron jobs
7. Run enricher against full DB, run review_scraper for first batch
