import re


def parse_price(raw: str) -> int:
    """'1.500.000đ' / '1,500,000 VND' / '1500000' → 1500000"""
    digits = re.sub(r"[^\d]", "", str(raw))
    return int(digits) if digits else 0


def parse_capacity_gb(raw: str) -> int:
    """'8GB' / '8gb' / '8192MB' / '1TB' → int GB"""
    raw = str(raw).upper().strip()
    tb = re.search(r"([\d.]+)\s*TB", raw)
    if tb:
        return int(float(tb.group(1)) * 1024)
    gb = re.search(r"([\d.]+)\s*GB", raw)
    if gb:
        return int(float(gb.group(1)))
    mb = re.search(r"([\d.]+)\s*MB", raw)
    if mb:
        return int(float(mb.group(1)) / 1024)
    digits = re.search(r"\d+", raw)
    return int(digits.group()) if digits else 0


def parse_speed_mhz(raw: str) -> int:
    """'3200MHz' / '3200 MHz' / 'DDR4-3200' → 3200"""
    m = re.search(r"(\d{3,5})\s*(?:MHz|mhz|MHZ)?", str(raw))
    return int(m.group(1)) if m else 0


def parse_clock_ghz(raw: str) -> float:
    """'3.6GHz' / '3600MHz' / '3.6' → 3.6"""
    raw = str(raw).upper()
    ghz = re.search(r"([\d.]+)\s*GHZ", raw)
    if ghz:
        return round(float(ghz.group(1)), 2)
    mhz = re.search(r"([\d]{3,5})\s*MHZ", raw)
    if mhz:
        return round(float(mhz.group(1)) / 1000, 2)
    plain = re.search(r"[\d.]+", raw)
    val = float(plain.group()) if plain else 0.0
    return round(val / 1000 if val > 100 else val, 2)


def parse_tdp_watts(raw: str) -> int:
    """'125W' / '125 W' / '125 Watts' → 125"""
    m = re.search(r"(\d+)\s*[Ww]", str(raw))
    return int(m.group(1)) if m else 0


def parse_length_mm(raw: str) -> int:
    """'320mm' / '320 mm' / '32cm' → 320"""
    raw = str(raw).upper()
    mm = re.search(r"(\d+)\s*MM", raw)
    if mm:
        return int(mm.group(1))
    cm = re.search(r"(\d+)\s*CM", raw)
    if cm:
        return int(cm.group(1)) * 10
    digits = re.search(r"\d+", raw)
    return int(digits.group()) if digits else 0


def parse_wattage(raw: str) -> int:
    """'750W' / '750 W' → 750"""
    m = re.search(r"(\d+)\s*[Ww]", str(raw))
    return int(m.group(1)) if m else 0


def normalize_socket(raw: str) -> str:
    """'Socket AM5' / 'socket am5' / 'AM5' → 'AM5'; 'LGA 1700' → 'LGA1700'"""
    raw = re.sub(r"(?i)socket\s*", "", str(raw)).strip().upper()
    raw = re.sub(r"\s+", "", raw)
    return raw


def normalize_memory_type(raw: str) -> str:
    """'DDR5-5600' / 'ddr4' / 'DDR 4' → 'DDR5' or 'DDR4'"""
    m = re.search(r"DDR\s*(\d)", str(raw).upper())
    return f"DDR{m.group(1)}" if m else "DDR4"


def normalize_form_factor(raw: str) -> str:
    """'Micro-ATX' / 'mATX' / 'micro atx' → 'mATX'"""
    raw = str(raw).upper().replace("-", "").replace(" ", "")
    if "MICROATX" in raw or "MATX" in raw:
        return "mATX"
    if "MINIITX" in raw or "ITX" in raw:
        return "ITX"
    return "ATX"


def normalize_efficiency(raw: str) -> str:
    """'80 Plus Gold' / '80+ Bronze' → '80+ Gold'"""
    raw = str(raw).upper()
    for tier in ["TITANIUM", "PLATINUM", "GOLD", "SILVER", "BRONZE"]:
        if tier in raw:
            return f"80+ {tier.capitalize()}"
    return "80+"


def extract_manufacturer_from_name(name: str) -> str:
    """Best-effort brand extraction from product name."""
    brands = [
        "Intel", "AMD", "ASUS", "MSI", "Gigabyte", "ASRock", "Biostar",
        "Corsair", "Kingston", "G.Skill", "Samsung", "SK Hynix", "Crucial",
        "NVIDIA", "Gainward", "Zotac", "Palit", "Galax", "KFA2",
        "Seasonic", "Cooler Master", "be quiet!", "Thermaltake", "Antec",
        "Noctua", "DeepCool", "Lian Li", "Fractal", "NZXT", "Phanteks",
        "Western Digital", "WD", "Seagate", "Toshiba", "Lexar",
    ]
    name_lower = name.lower()
    for brand in brands:
        if brand.lower() in name_lower:
            return brand
    return name.split()[0] if name else "Unknown"
