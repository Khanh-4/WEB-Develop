import time
import requests
from bs4 import BeautifulSoup
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.by import By


HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/124.0.0.0 Safari/537.36"
    ),
    "Accept-Language": "vi-VN,vi;q=0.9,en;q=0.8",
}


class StaticScraper:
    """For pages that don't require JS rendering."""

    def get_soup(self, url: str, retries: int = 3) -> BeautifulSoup | None:
        for attempt in range(retries):
            try:
                resp = requests.get(url, headers=HEADERS, timeout=15)
                resp.raise_for_status()
                return BeautifulSoup(resp.text, "lxml")
            except Exception as e:
                print(f"  [retry {attempt+1}/{retries}] {url} — {e}")
                time.sleep(2 ** attempt)
        return None

    def get_all_pages(self, url_template: str, max_pages: int = 20) -> list[BeautifulSoup]:
        """Fetch paginated pages. url_template must contain {page} placeholder."""
        pages = []
        for page in range(1, max_pages + 1):
            url = url_template.format(page=page)
            soup = self.get_soup(url)
            if soup is None:
                break
            # Stop if no products found on page
            if not self._has_products(soup):
                break
            pages.append(soup)
            time.sleep(0.8)
        return pages

    def _has_products(self, soup: BeautifulSoup) -> bool:
        return True  # Override in subclass


class DynamicScraper:
    """For JS-rendered pages (e.g. infinite scroll, dynamic pagination)."""

    def __init__(self, headless: bool = True):
        options = Options()
        if headless:
            options.add_argument("--headless=new")
        options.add_argument("--no-sandbox")
        options.add_argument("--disable-dev-shm-usage")
        options.add_argument("--disable-gpu")
        options.add_argument(f"user-agent={HEADERS['User-Agent']}")
        self.driver = webdriver.Chrome(options=options)
        self.wait = WebDriverWait(self.driver, 10)

    def get_soup(self, url: str) -> BeautifulSoup:
        self.driver.get(url)
        time.sleep(2)
        return BeautifulSoup(self.driver.page_source, "lxml")

    def click_next_page(self, selector: str) -> bool:
        """Click next-page button. Returns False if not found."""
        try:
            btn = self.wait.until(EC.element_to_be_clickable((By.CSS_SELECTOR, selector)))
            self.driver.execute_script("arguments[0].click();", btn)
            time.sleep(1.5)
            return True
        except Exception:
            return False

    def quit(self):
        self.driver.quit()
