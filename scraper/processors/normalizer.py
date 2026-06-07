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


def normalize_chipset(raw: str, name: str = "") -> str:
    """Extract motherboard chipset from specs or product name."""
    combined = (str(raw) + " " + str(name)).upper()
    intel = ["Z890", "Z790", "Z690", "Z590", "Z490", "B860", "B760", "B660", "B560",
             "H870", "H770", "H670", "H610", "H570", "H510", "W790", "W680", "X299"]
    amd   = ["X870E", "X870", "X670E", "X670", "B850", "B650E", "B650", "A620",
             "X570", "B550", "X470", "B450", "A520"]
    for c in intel + amd:
        if c in combined:
            return c
    return ""


def normalize_ram_profile(raw: str, name: str = "") -> str:
    """Detect XMP / Expo overclock profile from specs or product name."""
    combined = (str(raw) + " " + str(name)).upper()
    if "EXPO" in combined:
        return "AMD Expo"
    if "XMP 3" in combined or "XMP3" in combined:
        return "Intel XMP 3.0"
    if "XMP 2" in combined or "XMP2" in combined:
        return "Intel XMP 2.0"
    if "XMP" in combined:
        return "Intel XMP"
    return ""


def normalize_psu_form_factor(raw: str, name: str = "") -> str:
    """Detect PSU physical form factor (ATX vs SFX vs TFX)."""
    combined = (str(raw) + " " + str(name)).upper()
    if "SFX-L" in combined or "SFXL" in combined:
        return "SFX-L"
    if "SFX" in combined:
        return "SFX"
    if "TFX" in combined:
        return "TFX"
    return "ATX"


def normalize_case_type(raw: str, name: str = "") -> str:
    """Detect case tower type from specs or name."""
    combined = (str(raw) + " " + str(name)).upper()
    if "FULL" in combined:
        return "Full Tower"
    if "MINI" in combined or "SMALL" in combined or "MINI-ITX" in combined:
        return "Mini Tower"
    if "MID" in combined or "MIDI" in combined or "MICRO" in combined:
        return "Mid Tower"
    # Fallback: ITX-only cases are mini
    if "ITX" in combined and "ATX" not in combined:
        return "Mini Tower"
    return "Mid Tower"


def parse_radiator_support(specs: dict, name: str = "") -> str:
    """Return comma-separated radiator sizes supported by the case."""
    combined = (" ".join(specs.values()) + " " + name).upper()
    found = []
    for mm in ["420", "360", "280", "240", "120"]:
        pattern = rf"\b{mm}\s*MM\b|{mm}MM|\b{mm}\b"
        if re.search(pattern, combined):
            found.append(f"{mm}mm")
    return ", ".join(found)


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
