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
