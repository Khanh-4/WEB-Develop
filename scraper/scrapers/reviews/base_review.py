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
