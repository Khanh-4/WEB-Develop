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
    "gpu":         r'(RTX\s+\d[\d\w]*(?:\s+\w+){0,3}|RX\s+\d[\d\w]*(?:\s+\w+){0,3}|GTX\s+\d[\d\w]*(?:\s+\w+){0,3}|Arc\s+[A-Z]\d+)',
    "cpu":         r'(i[3579]-\d+\w*|Ryzen\s+[3579]\s+\d+\w*)',
    "ram":         r'(DDR[45][-\s]?\d{3,4}\w*)',
    "motherboard": r'([ZBH]\d{3}[A-Z0-9\-]*)',
    "storage":     r'(\d+\s?[GT]B\s+(?:NVMe|SSD|M\.2)|(?:NVMe|SSD|M\.2)\s+\d+\s?[GT]B)',
    "psu":         r'(\d{3,4}\s?W)',
    "cooler":      r'(NH-\w+|[A-Z]{2,4}-?\d{2,4}\w*)',
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
