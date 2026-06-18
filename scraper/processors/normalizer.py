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
    """Normalize a CPU/MB socket string to a canonical token matching Cpu.Socket.

    Handles brand prefixes and concatenation noise so values match across tables:
      'Socket AM5'/'am5' → 'AM5'; 'LGA 1700'/'1700' → 'LGA1700';
      'Intel LGA1700' → 'LGA1700'; 'AMDAM5' → 'AM5'; 'LGAAM5' → 'AM5'.
    Returns '' when unrecognized so callers can fall back to name inference.
    """
    s = re.sub(r"(?i)socket", "", str(raw)).upper()
    s = re.sub(r"[^A-Z0-9+]", "", s)                 # drop spaces, dashes, slashes
    s = s.replace("INTEL", "").replace("AMD", "")     # strip brand prefixes
    if not s or s == "UNKNOWN":
        return ""
    # AMD first (so a stray 'LGA' prefix on 'LGAAM5' is discarded)
    if "AM5" in s:
        return "AM5"
    if "AM4" in s:
        return "AM4"
    if "AM3+" in s:
        return "AM3+"
    # Intel LGA: keep the numeric pad code (1700/1851/1200/1151/...)
    m = re.search(r"\d{3,4}", s)
    if m and ("LGA" in s or re.fullmatch(r"\d{3,4}", s)):
        return "LGA" + m.group()
    if s.startswith("LGA"):
        return s
    return ""


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


# Canonical brand -> list of case-insensitive regex patterns.
# Order matters: multi-word / more-specific brands come first so they win over
# generic substrings (e.g. "Cooler Master" before "MSI"). Patterns use word
# boundaries to avoid false hits ("\bWD\b" must not fire inside "FORWARD").
# Aliases collapse onto one canonical label so the brand filter shows a single
# entry (e.g. "WD" + "Western Digital" -> "Western Digital").
_BRAND_PATTERNS = [
    ("Western Digital", [r"western\s*digital", r"\bwd[_\s-]?black\b", r"\bwd\b"]),
    ("Cooler Master",   [r"cooler\s*master"]),
    ("Lian Li",         [r"lian[\s-]*li"]),
    ("be quiet!",       [r"be\s*quiet"]),
    ("Silicon Power",   [r"silicon\s*power"]),
    ("Super Flower",    [r"super\s*flower"]),
    ("ID-Cooling",      [r"id[\s-]*cooling"]),
    ("G.Skill",         [r"g[\s.]*skill"]),
    ("TeamGroup",       [r"team\s*group", r"\bt[\s-]*force\b", r"\bteam\b"]),
    ("Kingmax",         [r"kingmax"]),
    ("Kingston",        [r"kingston", r"\bhyperx\b"]),
    ("ADATA",           [r"\badata\b", r"\bxpg\b"]),
    ("Corsair",         [r"corsair"]),
    ("Patriot",         [r"patriot", r"\bviper\b"]),
    ("Apacer",          [r"apacer"]),
    ("GEIL",            [r"\bgeil\b"]),
    ("Crucial",         [r"crucial"]),
    ("Lexar",           [r"lexar"]),
    ("Klevv",           [r"klevv"]),
    ("SanDisk",         [r"sandisk"]),
    ("Transcend",       [r"transcend"]),
    ("Samsung",         [r"samsung"]),
    ("SK Hynix",        [r"sk\s*hynix", r"\bhynix\b"]),
    ("Seagate",         [r"seagate"]),
    ("Toshiba",         [r"toshiba"]),
    ("ASUS",            [r"\basus\b", r"\brog\b", r"\btuf\b"]),
    ("ASRock",          [r"asrock"]),
    ("Gigabyte",        [r"gigabyte", r"\baorus\b"]),
    ("MSI",             [r"\bmsi\b"]),
    ("Biostar",         [r"biostar"]),
    ("Colorful",        [r"colorful", r"\bigame\b"]),
    ("Inno3D",          [r"inno\s*3d"]),
    ("Sparkle",         [r"sparkle"]),
    ("Sapphire",        [r"sapphire"]),
    ("PowerColor",      [r"power\s*color"]),
    ("PNY",             [r"\bpny\b"]),
    ("Leadtek",         [r"leadtek"]),
    ("Gainward",        [r"gainward"]),
    ("Zotac",           [r"zotac"]),
    ("Palit",           [r"palit"]),
    ("Galax",           [r"galax", r"\bkfa2\b"]),
    ("Yeston",          [r"yeston"]),
    ("OCPC",            [r"\bocpc\b"]),
    ("NVIDIA",          [r"nvidia"]),
    ("Seasonic",        [r"seasonic"]),
    ("FSP",             [r"\bfsp\b"]),
    ("Antec",           [r"antec"]),
    ("Thermalright",    [r"thermalright"]),
    ("Thermaltake",     [r"thermaltake"]),
    ("NZXT",            [r"nzxt"]),
    ("Phanteks",        [r"phanteks"]),
    ("Fractal",         [r"fractal"]),
    ("DeepCool",        [r"deep\s*cool"]),
    ("Noctua",          [r"noctua"]),
    ("Arctic",          [r"\barctic\b"]),
    ("Xigmatek",        [r"xigmatek"]),
    ("Jonsbo",          [r"jonsbo"]),
    ("HYTE",            [r"\bhyte\b"]),
    ("Montech",         [r"montech"]),
    ("VITRA",           [r"\bvitra\b"]),
    ("KENOO",           [r"kenoo"]),
    ("MIK",             [r"\bmik\b"]),
    # Budget / Vietnam-market brands (common on these retailer sites).
    ("Golden Field",    [r"golden\s*field"]),
    ("AeroCool",        [r"aero\s*cool"]),
    ("KingSpec",        [r"king\s*spec"]),
    ("Kioxia",          [r"kioxia"]),
    ("HIKSEMI",         [r"hiksemi", r"hikvision"]),
    ("Darkflash",       [r"dark\s*flash"]),
    ("Huntkey",         [r"huntkey"]),
    ("Gamdias",         [r"gamdias"]),
    ("InWin",           [r"\bin[\s-]*win\b"]),
    ("AIGO",            [r"\baigo\b"]),
    ("Jetek",           [r"jetek"]),
    ("SAMA",            [r"\bsama\b"]),
    ("VSP",             [r"\bvsp\b"]),
    ("Arrow",           [r"\barrow\b"]),
    ("MAGIC",           [r"\bmagic\b"]),
    ("AGI",             [r"\bagi\b"]),
    ("Acer",            [r"\bacer\b", r"\bpredator\b"]),
    ("Intel",           [r"\bintel\b"]),
    ("AMD",             [r"\bamd\b", r"\bryzen\b", r"\bradeon\b"]),
]

# Pre-compile once: (canonical, compiled_regex) for each pattern.
_BRAND_COMPILED = [
    (canonical, re.compile(pat, re.IGNORECASE))
    for canonical, pats in _BRAND_PATTERNS
    for pat in pats
]


# Phrases that mark a listing as a full system / laptop rather than a discrete
# component — these pollute the cpu and video_card tables when a retailer mixes
# them into a component category page. "Workstation" is intentionally excluded:
# a "VGA ... Workstation Edition" is a legitimate graphics card.
_SYSTEM_RX = re.compile(
    r"\b(pc gvn|pc gaming|gaming pc|máy bộ|bộ máy|laptop|notebook|macbook)\b",
    re.IGNORECASE,
)


def is_system_or_laptop(name: str) -> bool:
    """True if the product name is a prebuilt PC or a laptop (not a component)."""
    return bool(name) and _SYSTEM_RX.search(name) is not None


def extract_manufacturer_from_name(name: str) -> str:
    """Best-effort brand extraction from a product name.

    Scans a curated, ordered allowlist of known PC-component brands (with
    aliases) and returns the canonical brand label. Returns "Khác" (Vietnamese
    for "Other") when nothing matches — never the first word of the name, which
    previously produced junk filter entries like "Liên", "RAM", "Vỏ", "Ổ".
    """
    if not name:
        return "Khác"
    for canonical, rx in _BRAND_COMPILED:
        if rx.search(name):
            return canonical
    return "Khác"
