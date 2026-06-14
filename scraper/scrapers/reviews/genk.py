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
from utils.html_clean import clean_article_html

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
                content=clean_article_html(body, article_url),
                source=SOURCE,
                thumbnail_url=thumb,
                published_at=pub_date,
            ))
            if len(articles) >= max_results:
                break

        return articles
