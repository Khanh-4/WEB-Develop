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
