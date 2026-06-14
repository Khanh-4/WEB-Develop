"""
Allowlist-based HTML sanitizer for scraped article content.
Uses nh3 (Rust/ammonia) to strip anything not on the explicit allowlist —
safer than blacklisting dangerous tags, which misses event handlers and
novel attack vectors like <svg onload>, <a href="javascript:">, etc.
"""
from urllib.parse import urljoin
import nh3
from bs4 import BeautifulSoup

_ALLOWED_TAGS = {
    "a", "abbr", "b", "blockquote", "br", "caption", "code", "col", "colgroup",
    "del", "details", "div", "em", "figure", "figcaption",
    "h1", "h2", "h3", "h4", "h5", "h6", "hr",
    "i", "img", "ins", "li", "mark", "ol", "p", "pre",
    "s", "span", "strong", "sub", "summary", "sup",
    "table", "tbody", "td", "th", "thead", "tr", "u", "ul",
}

_ALLOWED_ATTRS: dict[str, set[str]] = {
    "*":        {"class", "id"},
    "a":        {"href", "title", "rel"},
    "img":      {"src", "alt", "width", "height", "loading"},
    "td":       {"colspan", "rowspan"},
    "th":       {"colspan", "rowspan", "scope"},
    "col":      {"span"},
    "colgroup": {"span"},
}


def clean_article_html(soup: BeautifulSoup, base_url: str) -> str:
    """Rewrite img src to absolute URLs then sanitize with an allowlist."""
    for img in soup.find_all("img"):
        src = img.get("src") or img.get("data-src") or ""
        if src:
            img["src"] = urljoin(base_url, src)
            img["loading"] = "lazy"
        else:
            img.decompose()

    return nh3.clean(
        str(soup),
        tags=_ALLOWED_TAGS,
        attributes=_ALLOWED_ATTRS,
        url_schemes={"http", "https"},
        link_rel="noopener noreferrer",
    )
