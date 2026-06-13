# Data Enrichment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix missing spec fields across all 8 hardware categories and add a review article pipeline that scrapes full articles from Tinhte/GenK/Tom's Hardware, fuzzy-matches them to products, and displays them as a new "Bài đánh giá" tab on Product Detail.

**Architecture:** Spec enrichment works by (1) storing `SourceUrl` per product so any product can be re-fetched without a full catalog scrape, and (2) expanding `_find()` key lookups in each scraper so more spec table rows are captured. Review pipeline adds `scrapers/reviews/` module with a base class, three site-specific scrapers, and a `review_scraper.py` runner that fuzzy-matches article titles to DB products using rapidfuzz. The web side adds a `ProductArticle` model, a single GET endpoint, and a new accordion tab on Product Detail.

**Tech Stack:** Python 3.11 + SQLAlchemy + BeautifulSoup4 + rapidfuzz | ASP.NET Core 8 + EF Core | PostgreSQL (Supabase)

---

## File Map

### Scraper (`/scraper`)

| File | Action | Responsibility |
|------|--------|----------------|
| `models/hardware.py` | Modify | Add `SourceUrl` column to all 8 model classes |
| `main.py` | Modify | Add `SourceUrl` to `_SPEC_FIELDS`; store URL on new inserts |
| `scrapers/phongvu.py` | Modify | Expand `_find()` key lists; pass `SourceUrl` to model constructors |
| `scrapers/anphat.py` | Modify | Same as phongvu |
| `scrapers/ttgshop.py` | Modify | Same as phongvu |
| `scrapers/gearvn.py` | Modify | Same as phongvu |
| `scrapers/reviews/__init__.py` | Create | Empty package marker |
| `scrapers/reviews/base_review.py` | Create | `RawArticle` dataclass + `BaseReviewScraper` ABC |
| `scrapers/reviews/tinhte.py` | Create | Tinhte.vn article scraper |
| `scrapers/reviews/genk.py` | Create | GenK.vn article scraper |
| `scrapers/reviews/tomshardware.py` | Create | Tom's Hardware scraper |
| `utils/fuzzy_match.py` | Create | `match_article_to_product()` + `extract_short_name()` |
| `review_scraper.py` | Create | CLI runner: seed → search → match → insert |
| `requirements.txt` | Modify | Add `rapidfuzz` |

### Web (`/web`)

| File | Action | Responsibility |
|------|--------|----------------|
| `Models/Cpu.cs` (+ 7 others) | Modify | Add `public string? SourceUrl { get; set; }` |
| `Models/ProductArticle.cs` | Create | EF model for `product_articles` table |
| `Data/AppDbContext.cs` | Modify | Add `DbSet<ProductArticle> ProductArticles` |
| `Data/Migrations/` | Generate | `AddProductArticles` + `AddSourceUrlToHardwareTables` |
| `ViewModels/ProductArticleDto.cs` | Create | DTO for API response |
| `Controllers/ProductsController.cs` | Modify | Add `GET /Products/Articles` endpoint |
| `Views/Products/Detail.cshtml` | Modify | Add "Bài đánh giá" tab + article accordion list |

### GitHub Actions

| File | Action |
|------|--------|
| `.github/workflows/scraper.yml` | Modify — add weekly jobs for review_scraper |

---

## Task 1: Add SourceUrl to Python hardware models

**Files:**
- Modify: `scraper/models/hardware.py`
- Modify: `scraper/main.py`

- [ ] **Step 1: Add SourceUrl column to all 8 model classes**

In `scraper/models/hardware.py`, add `SourceUrl = Column(String, nullable=True)` as the last column of each class (before the end of the class body). Apply to: `Cpu`, `Motherboard`, `Memory`, `VideoCard`, `PowerSupply`, `CaseEnclosure`, `Storage`, `CpuCooler`.

Example for `Cpu`:
```python
class Cpu(Base):
    __tablename__ = "cpu"

    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String(200), nullable=False)
    Manufacturer = Column(String(100), nullable=False)
    Price = Column(Numeric(18, 0), nullable=False)
    Socket = Column(String(50), nullable=False)
    CoreCount = Column(Integer, nullable=False, default=0)
    ThreadCount = Column(Integer, nullable=False, default=0)
    BaseClock = Column(Numeric(5, 2), nullable=False, default=0)
    BoostClock = Column(Numeric(5, 2), nullable=False, default=0)
    TDP = Column(Integer, nullable=False, default=0)
    ApproximatePerformance = Column(Numeric(10, 2), nullable=False, default=0)
    ImageUrl = Column(String, nullable=True)
    Stock = Column(Integer, nullable=False, default=0)
    SourceUrl = Column(String, nullable=True)   # ← add to all 8 classes
```

- [ ] **Step 2: Add SourceUrl to `_SPEC_FIELDS` in main.py**

In `scraper/main.py`, append `"SourceUrl"` to each category's list in `_SPEC_FIELDS`:
```python
_SPEC_FIELDS: dict[str, list[str]] = {
    "motherboard":    ["Chipset", "SocketCompatibility", "FormFactor", "MemoryCompatibility",
                       "MemorySlots", "MaxMemoryCapacity", "ImageUrl", "SourceUrl"],
    "cpu":            ["Socket", "CoreCount", "ThreadCount", "BaseClock", "BoostClock",
                       "TDP", "ApproximatePerformance", "ImageUrl", "SourceUrl"],
    "video_card":     ["VRAM", "Length", "TDP", "ApproximatePerformance", "ImageUrl", "SourceUrl"],
    "memory":         ["Type", "Capacity", "Modules", "Speed", "Profile", "ImageUrl", "SourceUrl"],
    "storage":        ["Type", "Capacity", "Interface", "ReadSpeed", "WriteSpeed", "ImageUrl", "SourceUrl"],
    "power_supply":   ["Wattage", "Efficiency", "Modular", "PsuFormFactor", "ImageUrl", "SourceUrl"],
    "case_enclosure": ["FormFactorSupport", "MaxVGALength", "Color", "CaseType",
                       "RadiatorSupport", "ImageUrl", "SourceUrl"],
    "cpu_cooler":     ["SocketCompatibility", "MaxTDP", "Height", "Type", "ImageUrl", "SourceUrl"],
}
```

- [ ] **Step 3: Verify upsert SELECT includes SourceUrl**

`_SPEC_FIELDS` drives the SELECT in `upsert()` — since SourceUrl is now in the list, the SELECT will automatically include it and fill it in on re-scrape. No other change needed in `upsert()`.

- [ ] **Step 4: Commit**

```bash
git add scraper/models/hardware.py scraper/main.py
git commit -m "feat(scraper): add SourceUrl field to all 8 hardware models"
```

---

## Task 2: Pass SourceUrl from PhongVu scraper

**Files:**
- Modify: `scraper/scrapers/phongvu.py`

PhongVu already stores `basic["url"]` during `parse_card()`. We just need to pass it to each model constructor.

- [ ] **Step 1: Add SourceUrl to every model constructor in phongvu.py**

Search for every `results.append(Cpu(...)`, `results.append(Motherboard(...)`, etc. in `phongvu.py`. Add `SourceUrl=basic["url"]` to each. There are 8 scrape functions, one per category.

Example for `scrape_cpus()`:
```python
results.append(Cpu(
    Name=name,
    Manufacturer=extract_manufacturer_from_name(name),
    Price=basic["price"],
    Socket=socket or "Unknown",
    CoreCount=cores,
    ThreadCount=threads,
    BaseClock=base_c or 0,
    BoostClock=boost_c or 0,
    TDP=tdp or tdp_fallback,
    ApproximatePerformance=score_cpu(cores, boost_c or base_c or 0),
    ImageUrl=basic["image"],
    Stock=1,
    SourceUrl=basic["url"],   # ← add this line
))
```

Apply the same pattern to all 8 scrape functions in phongvu.py.

- [ ] **Step 2: Expand spec key lookups for commonly-missed fields**

In `scrape_motherboards()`, the chipset line is:
```python
chipset = normalize_chipset(_find(specs, "chipset", "chip", "vi điều khiển"), name)
```

Expand to cover more PhongVu key variations:
```python
chipset = normalize_chipset(
    _find(specs, "chipset", "chip", "vi điều khiển", "loại chipset", "chip set", "nhà sản xuất chip"), name
)
socket = normalize_socket(
    _find(specs, "socket", "loại socket", "cpu socket", "socket cpu", "loại cpu")
)
ff = normalize_form_factor(
    _find(specs, "form factor", "kích thước", "chuẩn", "chuẩn board", "kích thước bo mạch")
)
```

In `scrape_video_cards()`, expand length and TDP lookups:
```python
length = parse_length_mm(_find(specs, "chiều dài", "card length", "length", "độ dài card", "kích thước card"))
tdp    = parse_tdp_watts(_find(specs, "tdp", "công suất", "power consumption", "tiêu thụ điện", "mức tiêu thụ"))
```

In `scrape_cases()`, expand MaxVGALength lookup:
```python
max_vga = parse_length_mm(_find(specs,
    "chiều dài vga tối đa", "max gpu length", "max vga length",
    "độ dài vga tối đa", "hỗ trợ vga", "vga tối đa"
))
```

In `scrape_cpu_coolers()`, expand socket compatibility lookup:
```python
socket_str = _find(specs,
    "socket support", "socket hỗ trợ", "socket tương thích",
    "tương thích socket", "loại socket", "socket"
)
```

- [ ] **Step 3: Commit**

```bash
git add scraper/scrapers/phongvu.py
git commit -m "feat(scraper): store SourceUrl + expand spec key lookups in PhongVu scraper"
```

---

## Task 3: Pass SourceUrl from AnPhat, TTGShop, GearVN scrapers

**Files:**
- Modify: `scraper/scrapers/anphat.py`
- Modify: `scraper/scrapers/ttgshop.py`
- Modify: `scraper/scrapers/gearvn.py`

- [ ] **Step 1: Add SourceUrl to AnPhat model constructors**

In `anphat.py`, each product dict already contains the product URL (look for how it's fetched in the scraper — it will be a key like `url` or `product_url`). Add `SourceUrl=<url_var>` to every model constructor in all 8 scrape functions.

If AnPhat uses `basic["url"]` pattern same as PhongVu, the change is identical: add `SourceUrl=basic["url"]` to each `results.append(...)` call.

- [ ] **Step 2: Add SourceUrl to TTGShop model constructors**

Same pattern in `ttgshop.py` — add `SourceUrl=<url_var>` to each model constructor in all 8 scrape functions.

- [ ] **Step 3: Add SourceUrl to GearVN model constructors**

Same pattern in `gearvn.py`. GearVN URLs come from the sitemap (`all_urls`) — the individual product URL is passed into each scrape function. Add `SourceUrl=<url_var>` to each model constructor.

- [ ] **Step 4: Commit**

```bash
git add scraper/scrapers/anphat.py scraper/scrapers/ttgshop.py scraper/scrapers/gearvn.py
git commit -m "feat(scraper): store SourceUrl in AnPhat, TTGShop, GearVN scrapers"
```

---

## Task 4: Add rapidfuzz + review scraper base class

**Files:**
- Modify: `scraper/requirements.txt`
- Create: `scraper/scrapers/reviews/__init__.py`
- Create: `scraper/scrapers/reviews/base_review.py`
- Create: `scraper/utils/fuzzy_match.py`

- [ ] **Step 1: Add rapidfuzz to requirements.txt**

```
# In scraper/requirements.txt, add:
rapidfuzz
```

Install: `cd scraper && source venv/bin/activate && pip install rapidfuzz`

- [ ] **Step 2: Create package marker**

```bash
touch scraper/scrapers/reviews/__init__.py
```

- [ ] **Step 3: Create base_review.py**

Create `scraper/scrapers/reviews/base_review.py`:
```python
from abc import ABC, abstractmethod
from dataclasses import dataclass
from datetime import datetime


@dataclass
class RawArticle:
    url: str
    title: str
    content: str          # cleaned HTML
    source: str           # 'tinhte' | 'genk' | 'tomshardware'
    thumbnail_url: str | None = None
    published_at: datetime | None = None


class BaseReviewScraper(ABC):
    DELAY = 1.0   # seconds between requests

    @abstractmethod
    def search_articles(self, keyword: str, max_results: int = 3) -> list[RawArticle]:
        """Search for articles about a product keyword. Return up to max_results."""
        ...
```

- [ ] **Step 4: Create utils/fuzzy_match.py**

Create `scraper/utils/fuzzy_match.py`:
```python
import re
from rapidfuzz import process, fuzz
from sqlalchemy import text
from sqlalchemy.orm import Session

CATEGORY_TABLE = {
    "gpu": "video_card", "cpu": "cpu", "ram": "memory",
    "motherboard": "motherboard", "psu": "power_supply",
    "case": "case_enclosure", "storage": "storage", "cooler": "cpu_cooler",
}

# Regex to pull short model identifier from product/article title
_MODEL_PATTERNS: dict[str, str] = {
    "gpu":         r'(RTX\s+\d[\d\s\w]*|RX\s+\d[\d\s\w]*|GTX\s+\d[\d\s\w]*|Arc\s+[A-Z]\d+)',
    "cpu":         r'(i[3579]-\d+\w*|Ryzen\s+[3579]\s+\d+\w*)',
    "ram":         r'(DDR[45][-\s]?\d{3,4}\w*)',
    "motherboard": r'([ZBH]\d{3}[A-Z0-9\-]*)',
    "storage":     r'(\d+\s?[GT]B\s+(?:NVMe|SSD|M\.2)|(?:NVMe|SSD|M\.2)\s+\d+\s?[GT]B)',
    "psu":         r'(\d{3,4}\s?W)',
    "cooler":      r'(NH-|AIO|[A-Z]\d{3})',
    "case":        r'(Mid Tower|Full Tower|Mini ITX|ATX)',
}


def extract_short_name(text: str, category: str) -> str | None:
    pattern = _MODEL_PATTERNS.get(category)
    if not pattern:
        return None
    m = re.search(pattern, text, re.IGNORECASE)
    return m.group(1).strip() if m else None


def match_to_product(title: str, category: str, session: Session) -> tuple[int | None, float]:
    """
    Returns (product_id, score 0.0-1.0) or (None, 0.0) if no confident match.
    Threshold: score >= 0.75.
    """
    keyword = extract_short_name(title, category)
    if not keyword:
        return None, 0.0

    table = CATEGORY_TABLE.get(category)
    if not table:
        return None, 0.0

    # Anchor on first 2 tokens for precision ("RTX 4070", not just "RTX")
    anchor = " ".join(keyword.split()[:2])
    rows = session.execute(
        text(f'SELECT "Id", "Name" FROM "{table}" WHERE "Name" ILIKE :q LIMIT 20'),
        {"q": f"%{anchor}%"},
    ).fetchall()

    if not rows:
        return None, 0.0

    candidates = {str(row[0]): row[1] for row in rows}
    result = process.extractOne(keyword, candidates, scorer=fuzz.token_sort_ratio)
    if result and result[1] >= 75:
        return int(result[2]), round(result[1] / 100, 3)
    return None, 0.0
```

- [ ] **Step 5: Commit**

```bash
git add scraper/requirements.txt scraper/scrapers/reviews/__init__.py \
        scraper/scrapers/reviews/base_review.py scraper/utils/fuzzy_match.py
git commit -m "feat(scraper): add review scraper base class and fuzzy match utility"
```

---

## Task 5: Tinhte scraper

**Files:**
- Create: `scraper/scrapers/reviews/tinhte.py`

- [ ] **Step 1: Create tinhte.py**

Create `scraper/scrapers/reviews/tinhte.py`:
```python
"""
Tinhte.vn review scraper.
Search URL: https://tinhte.vn/search?q={keyword}
Article pages: https://tinhte.vn/thread/{slug}.{id}/
"""
import time
import re
from datetime import datetime, timezone
from urllib.parse import urljoin, quote_plus
import requests
from bs4 import BeautifulSoup

from .base_review import BaseReviewScraper, RawArticle

BASE = "https://tinhte.vn"
HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
SOURCE = "tinhte"


def _get(url: str) -> BeautifulSoup | None:
    try:
        r = requests.get(url, headers=HEADERS, timeout=15)
        r.raise_for_status()
        return BeautifulSoup(r.text, "html.parser")
    except Exception as e:
        print(f"  [tinhte] GET failed {url}: {e}")
        return None


def _clean_html(soup: BeautifulSoup, base_url: str) -> str:
    """Strip scripts/ads; rewrite img src to absolute URLs."""
    for tag in soup(["script", "style", "iframe", "aside", "nav", "footer"]):
        tag.decompose()
    for img in soup.find_all("img"):
        src = img.get("src") or img.get("data-src") or ""
        if src:
            img["src"] = urljoin(base_url, src)
            img["loading"] = "lazy"
        if not img.get("src"):
            img.decompose()
    return str(soup)


def _parse_date(text: str) -> datetime | None:
    # Tinhte uses "DD/MM/YYYY" or ISO datetime attribute
    for fmt in ("%d/%m/%Y", "%Y-%m-%dT%H:%M:%S"):
        try:
            return datetime.strptime(text.strip()[:19], fmt).replace(tzinfo=timezone.utc)
        except ValueError:
            continue
    return None


class TinhteReviewScraper(BaseReviewScraper):
    def search_articles(self, keyword: str, max_results: int = 3) -> list[RawArticle]:
        search_url = f"{BASE}/search?q={quote_plus(keyword)}"
        soup = _get(search_url)
        if not soup:
            return []

        articles = []
        # Tinhte search results: look for article links in result list
        # Primary selector — adjust if site layout changes
        for link in soup.select("h3 a[href*='/thread/'], h2 a[href*='/thread/']")[:max_results * 2]:
            href = link.get("href", "")
            article_url = urljoin(BASE, href)
            title = link.get_text(strip=True)
            if not title or not article_url:
                continue

            time.sleep(self.DELAY)
            article_soup = _get(article_url)
            if not article_soup:
                continue

            # Article body
            body = (
                article_soup.select_one("div.article-content")
                or article_soup.select_one("div.message-body")
                or article_soup.select_one("div.bbWrapper")
            )
            if not body:
                continue

            # Date
            date_el = article_soup.select_one("time[datetime]")
            pub_date = None
            if date_el:
                pub_date = _parse_date(date_el.get("datetime", ""))

            # Thumbnail
            thumb = None
            og = article_soup.select_one('meta[property="og:image"]')
            if og:
                thumb = og.get("content")

            content_html = _clean_html(body, article_url)
            articles.append(RawArticle(
                url=article_url,
                title=title,
                content=content_html,
                source=SOURCE,
                thumbnail_url=thumb,
                published_at=pub_date,
            ))

            if len(articles) >= max_results:
                break

        return articles
```

- [ ] **Step 2: Quick smoke test**

```bash
cd scraper && source venv/bin/activate
python -c "
from scrapers.reviews.tinhte import TinhteReviewScraper
s = TinhteReviewScraper()
arts = s.search_articles('RTX 4070 Ti', max_results=1)
for a in arts:
    print(a.url, '|', a.title[:60], '|', len(a.content), 'chars')
"
```

Expected: prints 1 article URL + title. If 0 results, the selectors need adjusting — inspect the search result page HTML for the correct link selector.

- [ ] **Step 3: Commit**

```bash
git add scraper/scrapers/reviews/tinhte.py
git commit -m "feat(scraper): add Tinhte.vn review scraper"
```

---

## Task 6: GenK scraper

**Files:**
- Create: `scraper/scrapers/reviews/genk.py`

- [ ] **Step 1: Create genk.py**

Create `scraper/scrapers/reviews/genk.py`:
```python
"""
GenK.vn review scraper.
Search URL: https://genk.vn/search/?q={keyword}
"""
import time
from datetime import datetime, timezone
from urllib.parse import urljoin, quote_plus
import requests
from bs4 import BeautifulSoup

from .base_review import BaseReviewScraper, RawArticle

BASE = "https://genk.vn"
HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
SOURCE = "genk"


def _get(url: str) -> BeautifulSoup | None:
    try:
        r = requests.get(url, headers=HEADERS, timeout=15)
        r.raise_for_status()
        return BeautifulSoup(r.text, "html.parser")
    except Exception as e:
        print(f"  [genk] GET failed {url}: {e}")
        return None


def _clean_html(soup: BeautifulSoup, base_url: str) -> str:
    for tag in soup(["script", "style", "iframe", "aside"]):
        tag.decompose()
    for img in soup.find_all("img"):
        src = img.get("src") or img.get("data-src") or ""
        if src:
            img["src"] = urljoin(base_url, src)
            img["loading"] = "lazy"
    return str(soup)


def _parse_date(text: str) -> datetime | None:
    for fmt in ("%d/%m/%Y %H:%M", "%Y-%m-%dT%H:%M:%S", "%d/%m/%Y"):
        try:
            return datetime.strptime(text.strip()[:19], fmt).replace(tzinfo=timezone.utc)
        except ValueError:
            continue
    return None


class GenkReviewScraper(BaseReviewScraper):
    def search_articles(self, keyword: str, max_results: int = 3) -> list[RawArticle]:
        search_url = f"{BASE}/search/?q={quote_plus(keyword)}"
        soup = _get(search_url)
        if not soup:
            return []

        articles = []
        # GenK search results — article links in result items
        for item in soup.select("div.item-search, li.item-news")[:max_results * 2]:
            link = item.select_one("a[href]")
            if not link:
                continue
            article_url = urljoin(BASE, link["href"])
            title = link.get_text(strip=True) or item.select_one("h3, h2")
            if not title:
                title_el = item.select_one("h3, h2, .title")
                title = title_el.get_text(strip=True) if title_el else ""
            if not title:
                continue

            time.sleep(self.DELAY)
            article_soup = _get(article_url)
            if not article_soup:
                continue

            body = (
                article_soup.select_one("div.detail-content")
                or article_soup.select_one("div.article-body")
                or article_soup.select_one("div#content-detail")
            )
            if not body:
                continue

            date_el = (
                article_soup.select_one("time[datetime]")
                or article_soup.select_one("span.time-ago-last-edited")
                or article_soup.select_one("span.cms-date")
            )
            pub_date = _parse_date(date_el.get("datetime", "") or date_el.get_text()) if date_el else None

            og = article_soup.select_one('meta[property="og:image"]')
            thumb = og.get("content") if og else None

            articles.append(RawArticle(
                url=article_url,
                title=title,
                content=_clean_html(body, article_url),
                source=SOURCE,
                thumbnail_url=thumb,
                published_at=pub_date,
            ))
            if len(articles) >= max_results:
                break

        return articles
```

- [ ] **Step 2: Quick smoke test**

```bash
cd scraper && source venv/bin/activate
python -c "
from scrapers.reviews.genk import GenkReviewScraper
s = GenkReviewScraper()
arts = s.search_articles('GPU RTX 4070', max_results=1)
for a in arts:
    print(a.url, '|', a.title[:60])
"
```

Expected: prints 1 article. If 0, inspect GenK search result HTML for correct selectors.

- [ ] **Step 3: Commit**

```bash
git add scraper/scrapers/reviews/genk.py
git commit -m "feat(scraper): add GenK.vn review scraper"
```

---

## Task 7: Tom's Hardware scraper

**Files:**
- Create: `scraper/scrapers/reviews/tomshardware.py`

- [ ] **Step 1: Create tomshardware.py**

Create `scraper/scrapers/reviews/tomshardware.py`:
```python
"""
Tom's Hardware review scraper.
Search URL: https://www.tomshardware.com/search?q={keyword}&type=review
"""
import time
from datetime import datetime, timezone
from urllib.parse import urljoin, quote_plus
import requests
from bs4 import BeautifulSoup

from .base_review import BaseReviewScraper, RawArticle

BASE = "https://www.tomshardware.com"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "Accept-Language": "en-US,en;q=0.9",
}
SOURCE = "tomshardware"


def _get(url: str) -> BeautifulSoup | None:
    try:
        r = requests.get(url, headers=HEADERS, timeout=20)
        r.raise_for_status()
        return BeautifulSoup(r.text, "html.parser")
    except Exception as e:
        print(f"  [tomshardware] GET failed {url}: {e}")
        return None


def _clean_html(soup: BeautifulSoup, base_url: str) -> str:
    for tag in soup(["script", "style", "iframe", "aside", "nav",
                     "div.ad-unit", "div.newsletter", "div.related-articles"]):
        tag.decompose()
    for img in soup.find_all("img"):
        src = img.get("src") or img.get("data-src") or ""
        if src:
            img["src"] = urljoin(base_url, src)
            img["loading"] = "lazy"
    return str(soup)


def _parse_date(text: str) -> datetime | None:
    for fmt in ("%Y-%m-%dT%H:%M:%S", "%B %d, %Y", "%d %B %Y"):
        try:
            return datetime.strptime(text.strip()[:25], fmt).replace(tzinfo=timezone.utc)
        except ValueError:
            continue
    return None


class TomsHardwareReviewScraper(BaseReviewScraper):
    def search_articles(self, keyword: str, max_results: int = 3) -> list[RawArticle]:
        search_url = f"{BASE}/search?q={quote_plus(keyword)}&type=review"
        soup = _get(search_url)
        if not soup:
            return []

        articles = []
        # Tom's Hardware search results
        for item in soup.select("li.listingResult, div.search-result, article")[:max_results * 2]:
            link = item.select_one("a[href]")
            if not link:
                continue
            href = link["href"]
            article_url = urljoin(BASE, href) if not href.startswith("http") else href
            title_el = item.select_one("h3, h2, .article-name")
            title = title_el.get_text(strip=True) if title_el else link.get_text(strip=True)
            if not title:
                continue

            time.sleep(self.DELAY)
            article_soup = _get(article_url)
            if not article_soup:
                continue

            body = (
                article_soup.select_one("div#article-body")
                or article_soup.select_one("div.article-body")
                or article_soup.select_one("section.article-content")
            )
            if not body:
                continue

            date_el = article_soup.select_one("time[datetime]")
            pub_date = None
            if date_el:
                pub_date = _parse_date(date_el.get("datetime", ""))

            og = article_soup.select_one('meta[property="og:image"]')
            thumb = og.get("content") if og else None

            articles.append(RawArticle(
                url=article_url,
                title=title,
                content=_clean_html(body, article_url),
                source=SOURCE,
                thumbnail_url=thumb,
                published_at=pub_date,
            ))
            if len(articles) >= max_results:
                break

        return articles
```

- [ ] **Step 2: Quick smoke test**

```bash
cd scraper && source venv/bin/activate
python -c "
from scrapers.reviews.tomshardware import TomsHardwareReviewScraper
s = TomsHardwareReviewScraper()
arts = s.search_articles('RTX 4070 Ti review', max_results=1)
for a in arts:
    print(a.url, '|', a.title[:60])
"
```

Expected: 1 article from tomshardware.com. If 0, check search result HTML structure.

- [ ] **Step 3: Commit**

```bash
git add scraper/scrapers/reviews/tomshardware.py
git commit -m "feat(scraper): add Tom's Hardware review scraper"
```

---

## Task 8: review_scraper.py runner

**Files:**
- Create: `scraper/review_scraper.py`

- [ ] **Step 1: Create review_scraper.py**

Create `scraper/review_scraper.py`:
```python
"""
Review article scraper.
Usage:
    python review_scraper.py                          # all categories, all sources
    python review_scraper.py cpu gpu                  # specific categories
    python review_scraper.py --source tinhte,genk     # specific sources
    python review_scraper.py cpu --source tomshardware
"""
import sys
import os
import time
from datetime import datetime, timezone
from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from sqlalchemy.orm import Session

load_dotenv()
engine = create_engine(os.environ["DATABASE_URL"], echo=False)

from scrapers.reviews.tinhte import TinhteReviewScraper
from scrapers.reviews.genk import GenkReviewScraper
from scrapers.reviews.tomshardware import TomsHardwareReviewScraper
from utils.fuzzy_match import match_to_product, extract_short_name, CATEGORY_TABLE

ALL_CATS    = ["cpu", "gpu", "ram", "motherboard", "psu", "case", "storage", "cooler"]
ALL_SOURCES = ["tinhte", "genk", "tomshardware"]

SCRAPERS = {
    "tinhte":       TinhteReviewScraper(),
    "genk":         GenkReviewScraper(),
    "tomshardware": TomsHardwareReviewScraper(),
}

# Products per category to search for (top by performance/price)
TOP_N = 50
MAX_ARTICLES_PER_PRODUCT_PER_SOURCE = 3


def get_top_products(session: Session, category: str) -> list[dict]:
    table = CATEGORY_TABLE.get(category)
    if not table:
        return []
    perf_col = "ApproximatePerformance" if category in ("cpu", "gpu") else "Price"
    rows = session.execute(
        text(f'SELECT "Id", "Name" FROM "{table}" ORDER BY "{perf_col}" DESC LIMIT :n'),
        {"n": TOP_N},
    ).fetchall()
    return [{"id": row[0], "name": row[1]} for row in rows]


def already_scraped(session: Session, url: str) -> bool:
    row = session.execute(
        text('SELECT 1 FROM product_articles WHERE "Url" = :url LIMIT 1'), {"url": url}
    ).fetchone()
    return row is not None


def insert_article(session: Session, article, product_id, category, score):
    session.execute(text("""
        INSERT INTO product_articles
            ("ProductCategory", "ProductId", "Source", "Url", "Title",
             "Content", "ThumbnailUrl", "PublishedAt", "ScrapedAt", "MatchScore")
        VALUES
            (:cat, :pid, :src, :url, :title, :content, :thumb, :pub, :scraped, :score)
        ON CONFLICT ("Url") DO NOTHING
    """), {
        "cat":     category,
        "pid":     product_id,
        "src":     article.source,
        "url":     article.url,
        "title":   article.title,
        "content": article.content,
        "thumb":   article.thumbnail_url,
        "pub":     article.published_at,
        "scraped": datetime.now(timezone.utc),
        "score":   score,
    })


def run(cats: list[str], sources: list[str]):
    with Session(engine) as session:
        for category in cats:
            print(f"\n=== {category.upper()} ===")
            products = get_top_products(session, category)
            print(f"  {len(products)} products to search for")

            for product in products:
                keyword = extract_short_name(product["name"], category)
                if not keyword:
                    continue

                for source in sources:
                    scraper = SCRAPERS[source]
                    print(f"  [{source}] searching: {keyword}")

                    try:
                        articles = scraper.search_articles(keyword, MAX_ARTICLES_PER_PRODUCT_PER_SOURCE)
                    except Exception as e:
                        print(f"    ERROR: {e}")
                        continue

                    for article in articles:
                        if already_scraped(session, article.url):
                            print(f"    skip (already scraped): {article.url}")
                            continue

                        pid, score = match_to_product(article.title, category, session)
                        insert_article(session, article, pid, category, score)
                        matched = f"→ product {pid} (score {score})" if pid else "→ unmatched"
                        print(f"    + {article.title[:60]} {matched}")

                    session.commit()

    print("\nDone.")


if __name__ == "__main__":
    args = sys.argv[1:]

    sources = ALL_SOURCES
    if "--source" in args:
        idx = args.index("--source")
        source_val = args[idx + 1] if idx + 1 < len(args) else ""
        sources = source_val.split(",")
        args = [a for i, a in enumerate(args) if i != idx and i != idx + 1]

    cats = [c.lower() for c in args] if args else ALL_CATS
    run(cats, sources)
```

- [ ] **Step 2: Commit**

```bash
git add scraper/review_scraper.py
git commit -m "feat(scraper): add review_scraper.py CLI runner"
```

---

## Task 9: Web — ProductArticle model + EF migration

**Files:**
- Create: `web/Models/ProductArticle.cs`
- Modify: `web/Data/AppDbContext.cs`
- Modify: `web/Models/Cpu.cs` (+ 7 others)
- Generate: EF Core migrations

- [ ] **Step 1: Add SourceUrl to all 8 C# hardware models**

In each of these files, add `public string? SourceUrl { get; set; }` after the `ImageUrl` property:
- `web/Models/Cpu.cs`
- `web/Models/Motherboard.cs`
- `web/Models/Memory.cs`
- `web/Models/VideoCard.cs`
- `web/Models/PowerSupply.cs`
- `web/Models/CaseEnclosure.cs`
- `web/Models/Storage.cs`
- `web/Models/CpuCooler.cs`

Example for `Cpu.cs` (add after the existing `ImageUrl` property):
```csharp
public string? ImageUrl { get; set; }
public string? SourceUrl { get; set; }   // ← add this
public int Stock { get; set; }
```

- [ ] **Step 2: Create ProductArticle.cs**

Create `web/Models/ProductArticle.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TechSpecs.Models;

[Index(nameof(Url), IsUnique = true)]   // required for ON CONFLICT DO NOTHING in review_scraper.py

[Table("product_articles")]
public class ProductArticle
{
    public int Id { get; set; }

    [MaxLength(20)]
    public string? ProductCategory { get; set; }   // 'gpu', 'cpu', etc.

    public int? ProductId { get; set; }             // matched product (null if unmatched)

    [Required, MaxLength(20)]
    public string Source { get; set; } = string.Empty;  // 'tinhte' | 'genk' | 'tomshardware'

    [Required]
    public string Url { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public string? ThumbnailUrl { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

    public double? MatchScore { get; set; }
}
```

- [ ] **Step 3: Add DbSet to AppDbContext**

In `web/Data/AppDbContext.cs`, add inside the class:
```csharp
public DbSet<ProductArticle> ProductArticles { get; set; }
```

- [ ] **Step 4: Generate and run migrations**

```bash
cd web
dotnet dotnet-ef migrations add AddProductArticles --output-dir Data/Migrations
dotnet dotnet-ef migrations add AddSourceUrlToHardwareTables --output-dir Data/Migrations
dotnet dotnet-ef database update
```

Expected: two new migration files created, DB updated successfully.

- [ ] **Step 5: Commit**

```bash
git add web/Models/ProductArticle.cs web/Models/Cpu.cs web/Models/Motherboard.cs \
        web/Models/Memory.cs web/Models/VideoCard.cs web/Models/PowerSupply.cs \
        web/Models/CaseEnclosure.cs web/Models/Storage.cs web/Models/CpuCooler.cs \
        web/Data/AppDbContext.cs web/Data/Migrations/
git commit -m "feat(web): add ProductArticle model + SourceUrl to hardware models + EF migrations"
```

---

## Task 10: Web — Articles endpoint

**Files:**
- Create: `web/ViewModels/ProductArticleDto.cs`
- Modify: `web/Controllers/ProductsController.cs`

- [ ] **Step 1: Create ProductArticleDto.cs**

Create `web/ViewModels/ProductArticleDto.cs`:
```csharp
namespace TechSpecs.ViewModels;

public class ProductArticleDto
{
    public int Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ThumbnailUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

- [ ] **Step 2: Add GET /Products/Articles to ProductsController**

In `web/Controllers/ProductsController.cs`, add the following action (near other AJAX endpoints):
```csharp
[HttpGet]
public async Task<IActionResult> Articles(string category, int id)
{
    var articles = await _db.ProductArticles
        .Where(a => a.ProductCategory == category && a.ProductId == id)
        .OrderByDescending(a => a.PublishedAt)
        .Take(10)
        .Select(a => new ProductArticleDto
        {
            Id            = a.Id,
            Source        = a.Source,
            Url           = a.Url,
            Title         = a.Title,
            Content       = a.Content,
            ThumbnailUrl  = a.ThumbnailUrl,
            PublishedAt   = a.PublishedAt,
        })
        .ToListAsync();

    return Json(articles);
}
```

- [ ] **Step 3: Commit**

```bash
git add web/ViewModels/ProductArticleDto.cs web/Controllers/ProductsController.cs
git commit -m "feat(web): add GET /Products/Articles endpoint"
```

---

## Task 11: Web — Product Detail "Bài đánh giá" tab

**Files:**
- Modify: `web/Views/Products/Detail.cshtml`

- [ ] **Step 1: Add tab trigger to existing tab nav**

In `Detail.cshtml`, find the existing tab nav (`<ul class="nav nav-tabs ...">` or similar). Add a new tab button after the existing tabs:

```html
<li class="nav-item" role="presentation">
    <button class="nav-link" id="reviews-tab" data-bs-toggle="tab"
            data-bs-target="#reviews-pane" type="button" role="tab">
        Bài đánh giá
        <span class="badge bg-secondary ms-1" id="articleCount" style="display:none"></span>
    </button>
</li>
```

- [ ] **Step 2: Add tab pane**

In the tab content section, add the new pane (after the last existing pane):
```html
<div class="tab-pane fade" id="reviews-pane" role="tabpanel">
    <div id="articlesList" class="mt-3">
        <div class="text-center py-4 text-muted">
            <div class="spinner-border spinner-border-sm me-2"></div>
            Đang tải bài đánh giá...
        </div>
    </div>
</div>
```

- [ ] **Step 3: Add JS to load articles on tab click**

At the bottom of `Detail.cshtml` (in the `@section Scripts` block), add:
```javascript
// Load review articles when tab is first clicked
document.getElementById('reviews-tab')?.addEventListener('shown.bs.tab', function () {
    loadArticles();
}, { once: true });

async function loadArticles() {
    const category = '@Model.Category';   // must be set in ViewModel
    const id       = '@Model.Id';
    const list     = document.getElementById('articlesList');

    try {
        const res  = await fetch(`/Products/Articles?category=${category}&id=${id}`);
        const data = await res.json();

        if (!data.length) {
            list.innerHTML = '<p class="text-muted py-3">Chưa có bài đánh giá cho sản phẩm này.</p>';
            return;
        }

        document.getElementById('articleCount').textContent = data.length;
        document.getElementById('articleCount').style.display = '';

        const sourceLabels = { tinhte: 'Tinhte', genk: 'GenK', tomshardware: "Tom's Hardware" };
        const sourceColors = { tinhte: 'primary', genk: 'warning', tomshardware: 'danger' };

        list.innerHTML = data.map((a, i) => `
            <div class="card mb-3 glass-sm">
                <div class="card-body">
                    <div class="d-flex align-items-start gap-3">
                        ${a.thumbnailUrl ? `<img src="${a.thumbnailUrl}" width="80" height="60"
                            style="object-fit:cover;border-radius:6px;flex-shrink:0" loading="lazy">` : ''}
                        <div class="flex-grow-1">
                            <span class="badge bg-${sourceColors[a.source] || 'secondary'} mb-1">
                                ${sourceLabels[a.source] || a.source}
                            </span>
                            <h6 class="mb-1">${a.title}</h6>
                            <small class="text-muted">${a.publishedAt
                                ? new Date(a.publishedAt).toLocaleDateString('vi-VN')
                                : ''}</small>
                            <div>
                                <button class="btn btn-link btn-sm px-0 mt-1"
                                        type="button"
                                        data-bs-toggle="collapse"
                                        data-bs-target="#article-${i}">
                                    Đọc bài đầy đủ ↓
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class="collapse mt-2" id="article-${i}">
                        <div class="article-content border-top pt-3">
                            ${a.content || ''}
                        </div>
                    </div>
                </div>
            </div>
        `).join('');
    } catch (e) {
        list.innerHTML = '<p class="text-danger">Lỗi tải bài đánh giá.</p>';
    }
}
```

Note: `@Model.Category` and `@Model.Id` must exist on the ViewModel. Verify the ViewModel has these (likely `ProductDetailViewModel`). If the category is called something else (e.g., `CategorySlug`), use that name.

- [ ] **Step 4: Add article-content CSS**

In `wwwroot/css/site.css`, add styling for injected article HTML:
```css
.article-content img { max-width: 100%; height: auto; border-radius: 6px; margin: .5rem 0; }
.article-content h2, .article-content h3 { font-size: 1.1rem; margin-top: 1.25rem; }
.article-content p { line-height: 1.7; }
.article-content table { font-size: .875rem; width: 100%; }
```

- [ ] **Step 5: Run app and test the tab**

```bash
cd web && dotnet run --launch-profile http
```

Open a product detail page with known articles in DB, click "Bài đánh giá" tab. Expected: articles list with source badges, collapse/expand works.

- [ ] **Step 6: Commit**

```bash
git add web/Views/Products/Detail.cshtml web/wwwroot/css/site.css
git commit -m "feat(web): add Bài đánh giá tab on Product Detail page"
```

---

## Task 12: GitHub Actions — weekly review scraper job

**Files:**
- Modify: `.github/workflows/scraper.yml`

- [ ] **Step 1: Add weekly cron job to scraper.yml**

Open `.github/workflows/scraper.yml`. After the existing matrix jobs, add a new job:

```yaml
  review-scraper:
    runs-on: ubuntu-latest
    if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch'
    defaults:
      run:
        working-directory: scraper
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.11'
          cache: 'pip'
          cache-dependency-path: scraper/requirements.txt
      - run: pip install -r requirements.txt
      - name: Run review scraper
        env:
          DATABASE_URL: ${{ secrets.DATABASE_URL }}
        run: python review_scraper.py
```

Change the schedule trigger to run weekly (e.g., every Sunday at 3am UTC):
```yaml
on:
  schedule:
    - cron: '0 */12 * * *'   # existing: every 12 hours (product scraper)
    - cron: '0 3 * * 0'      # new: every Sunday 3am UTC (review scraper)
  workflow_dispatch:
```

- [ ] **Step 2: Commit and push**

```bash
git add .github/workflows/scraper.yml
git commit -m "ci: add weekly review scraper GitHub Actions job"
git push
```

- [ ] **Step 3: Verify workflow syntax**

Go to GitHub → Actions → check that both workflows appear without syntax errors in the workflow list.

---

## Final: First data run

After all tasks are complete, run the scrapers to populate data:

```bash
cd scraper && source venv/bin/activate

# 1. Re-scrape PhongVu to fill SourceUrl + new spec fields
python main.py --source phongvu

# 2. Run review scraper for GPU and CPU first (most review content available)
python review_scraper.py gpu cpu --source tinhte
python review_scraper.py gpu cpu --source genk
python review_scraper.py gpu cpu --source tomshardware
```

Check results:
```bash
# Quick DB check (adjust to your psql access)
python -c "
from sqlalchemy import create_engine, text
from dotenv import load_dotenv; import os
load_dotenv()
eng = create_engine(os.environ['DATABASE_URL'])
with eng.connect() as c:
    n = c.execute(text('SELECT COUNT(*) FROM product_articles')).scalar()
    matched = c.execute(text('SELECT COUNT(*) FROM product_articles WHERE \"ProductId\" IS NOT NULL')).scalar()
    print(f'{n} articles total, {matched} matched to products ({matched/n*100:.0f}%)')
"
```

Expected: >0 articles, match rate >50% for GPU/CPU categories.
