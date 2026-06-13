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
