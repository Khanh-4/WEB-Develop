"""
Phong Vu scraper — https://phongvu.vn
All category pages are static HTML with ?page=N pagination.
45 products per page.
"""

import time
import re
import requests
from bs4 import BeautifulSoup

from processors.normalizer import (
    parse_price, parse_capacity_gb, parse_speed_mhz, parse_clock_ghz,
    parse_tdp_watts, parse_length_mm, parse_wattage,
    normalize_socket, normalize_memory_type, normalize_form_factor,
    normalize_efficiency, extract_manufacturer_from_name,
    normalize_chipset, normalize_ram_profile, normalize_psu_form_factor,
    normalize_case_type, parse_radiator_support,
)
from scoring.performance import score_cpu, score_gpu
from models.hardware import (
    Cpu, Motherboard, Memory, VideoCard,
    PowerSupply, CaseEnclosure, Storage, CpuCooler,
)

BASE    = "https://phongvu.vn"
HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

CATEGORY_URLS = {
    "cpu":         BASE + "/c/cpu?page={page}",
    "motherboard": BASE + "/c/mainboard-bo-mach-chu?page={page}",
    "memory":      BASE + "/c/ram?page={page}",
    "gpu":         BASE + "/c/vga-card-man-hinh?page={page}",
    "psu":         BASE + "/c/psu-nguon-may-tinh?page={page}",
    "case":        BASE + "/c/case?page={page}",
    "ssd":         BASE + "/c/o-cung-ssd?page={page}",
    "hdd":         BASE + "/c/o-cung-hdd?page={page}",
    "cooler":      BASE + "/c/tan-nhiet?page={page}",
}


def get_soup(url: str, retries: int = 3) -> BeautifulSoup | None:
    for attempt in range(retries):
        try:
            r = requests.get(url, headers=HEADERS, timeout=15)
            r.raise_for_status()
            return BeautifulSoup(r.text, "lxml")
        except Exception as e:
            print(f"  [retry {attempt+1}/{retries}] {url[:60]} — {e}")
            time.sleep(2 ** attempt)
    return None


def get_pages(category_key: str, max_pages: int = 20) -> list[BeautifulSoup]:
    pages = []
    template = CATEGORY_URLS[category_key]
    for page in range(1, max_pages + 1):
        soup = get_soup(template.format(page=page))
        if soup is None:
            break
        cards = soup.select("div.product-card")
        if not cards:
            break
        pages.append(soup)
        print(f"  page {page}: {len(cards)} cards")
        time.sleep(0.6)
    return pages


def get_cards(soup: BeautifulSoup) -> list[BeautifulSoup]:
    return soup.select("div.product-card")


def parse_card(card: BeautifulSoup) -> dict:
    name_el  = card.select_one(".att-product-card-title")
    price_el = card.select_one(".att-product-detail-retail-price")
    img_el   = card.select_one("img")
    link_el  = card.select_one("a[href]")

    name  = name_el.get_text(strip=True) if name_el else ""
    price = parse_price(price_el.get_text(strip=True)) if price_el else 0
    image = img_el.get("src") or img_el.get("data-src") if img_el else None
    href  = link_el["href"] if link_el else None
    url   = (href if href and href.startswith("http") else BASE + href) if href else None

    return {"name": name, "price": price, "image": image, "url": url}


def get_specs(product_url: str) -> dict[str, str]:
    """Fetch detail page and extract spec table."""
    if not product_url:
        return {}
    soup = get_soup(product_url)
    if not soup:
        return {}

    specs: dict[str, str] = {}
    # Phong Vu uses a spec table with th/td pairs
    for row in soup.select("tr"):
        cells = row.select("th, td")
        if len(cells) >= 2:
            k = cells[0].get_text(strip=True).lower().strip(":")
            v = cells[1].get_text(strip=True)
            if k and v:
                specs[k] = v

    # Also try div-based spec layout
    for item in soup.select("div[class*='spec'], li[class*='spec']"):
        text = item.get_text(separator="|", strip=True)
        if "|" in text:
            k, _, v = text.partition("|")
            specs[k.lower().strip(":")] = v.strip()

    return specs


# ── CPU ────────────────────────────────────────────────────────────────────────

def scrape_cpus(max_pages: int = 10) -> list[Cpu]:
    print("\n[CPU]")
    results = []
    for soup in get_pages("cpu", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs = get_specs(basic["url"])
            time.sleep(0.3)

            # Parse from name if specs missing (common for Phong Vu)
            name = basic["name"]
            socket  = normalize_socket(_find(specs, "socket", "socket cpu", "loại socket"))
            cores   = _int(_find(specs, "số nhân", "cores", "lõi"))
            threads = _int(_find(specs, "số luồng", "threads"))
            base_c  = parse_clock_ghz(_find(specs, "tốc độ cơ bản", "base clock", "xung nhịp cơ bản"))
            boost_c = parse_clock_ghz(_find(specs, "tốc độ boost", "boost clock", "max turbo", "tốc độ tối đa"))
            tdp     = parse_tdp_watts(_find(specs, "tdp", "công suất nhiệt", "mức tiêu thụ"))

            # Fallback: extract from product name
            if not socket:
                socket = _socket_from_name(name)
            if cores == 0:
                cores = _cores_from_name(name)
            if boost_c == 0:
                boost_c = _boost_clock_from_name(name)
            if base_c == 0 and boost_c > 0:
                base_c = round(boost_c * 0.85, 2)  # Estimate base ≈ 85% of boost
            if tdp == 0:
                tdp = _tdp_from_name(name)

            results.append(Cpu(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                Socket=socket or "Unknown",
                CoreCount=cores,
                ThreadCount=threads or cores * 2,
                BaseClock=base_c,
                BoostClock=boost_c,
                TDP=tdp,
                ApproximatePerformance=score_cpu(cores, base_c, boost_c, tdp),
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} CPUs")
    return results


# ── Motherboard ─────────────────────────────────────────────────────────────────

def scrape_motherboards(max_pages: int = 10) -> list[Motherboard]:
    print("\n[Motherboard]")
    results = []
    for soup in get_pages("motherboard", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs  = get_specs(basic["url"])
            time.sleep(0.3)
            name   = basic["name"]

            socket  = normalize_socket(_find(specs, "socket", "loại socket", "cpu socket"))
            ff      = normalize_form_factor(_find(specs, "form factor", "kích thước", "chuẩn"))
            memtype = normalize_memory_type(_find(specs, "loại ram", "memory type", "chuẩn ram") or name)
            slots   = _int(_find(specs, "số khe ram", "memory slots", "khe cắm ram")) or 4
            chipset = normalize_chipset(_find(specs, "chipset", "chip", "vi điều khiển"), name)

            if not socket:
                socket = _mb_socket_from_name(name)

            results.append(Motherboard(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                SocketCompatibility=socket or "Unknown",
                FormFactor=ff,
                MemoryCompatibility=memtype,
                MemorySlots=slots,
                MaxMemoryCapacity=128,
                Chipset=chipset,
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} Motherboards")
    return results


# ── Memory ──────────────────────────────────────────────────────────────────────

def scrape_memory(max_pages: int = 10) -> list[Memory]:
    print("\n[Memory]")
    results = []
    for soup in get_pages("memory", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs   = get_specs(basic["url"])
            time.sleep(0.3)
            name    = basic["name"]

            memtype  = normalize_memory_type(_find(specs, "loại ram", "type", "chuẩn ram") or name)
            capacity = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or name)
            speed    = parse_speed_mhz(_find(specs, "tốc độ", "speed", "bus", "xung nhịp") or name)
            modules  = _int(_find(specs, "số thanh", "kit")) or 1
            profile = normalize_ram_profile(_find(specs, "profile", "xmp", "expo") or "", name)

            results.append(Memory(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                Type=memtype,
                Capacity=capacity,
                Modules=modules,
                Speed=speed,
                Profile=profile,
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} Memory")
    return results


# ── GPU ──────────────────────────────────────────────────────────────────────────

def scrape_video_cards(max_pages: int = 10) -> list[VideoCard]:
    print("\n[GPU]")
    results = []
    for soup in get_pages("gpu", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs  = get_specs(basic["url"])
            time.sleep(0.3)
            name   = basic["name"]

            vram_spec = _find(specs, "bộ nhớ", "vram", "memory")
            vram   = parse_capacity_gb(vram_spec) if vram_spec else _vram_from_name(name)
            length = parse_length_mm(_find(specs, "chiều dài", "card length", "length"))
            tdp    = parse_tdp_watts(_find(specs, "tdp", "công suất", "power consumption"))

            results.append(VideoCard(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                VRAM=vram,
                Length=length or 280,
                TDP=tdp,
                ApproximatePerformance=score_gpu(name, vram, tdp),
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} GPUs")
    return results


# ── PSU ──────────────────────────────────────────────────────────────────────────

def scrape_power_supplies(max_pages: int = 10) -> list[PowerSupply]:
    print("\n[PSU]")
    results = []
    for soup in get_pages("psu", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs  = get_specs(basic["url"])
            time.sleep(0.3)
            name   = basic["name"]

            wattage    = parse_wattage(_find(specs, "công suất", "wattage") or name)
            efficiency = normalize_efficiency(_find(specs, "hiệu suất", "efficiency", "chứng nhận") or "")
            modular    = (_find(specs, "modular", "dạng cáp") or "Non")[:20]
            psu_ff = normalize_psu_form_factor(_find(specs, "form factor", "kích thước nguồn") or "", name)

            results.append(PowerSupply(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                Wattage=wattage,
                Efficiency=efficiency,
                Modular=modular,
                PsuFormFactor=psu_ff,
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} PSUs")
    return results


# ── Case ─────────────────────────────────────────────────────────────────────────

def scrape_cases(max_pages: int = 10) -> list[CaseEnclosure]:
    print("\n[Case]")
    results = []
    for soup in get_pages("case", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs   = get_specs(basic["url"])
            time.sleep(0.3)
            name    = basic["name"]

            ff_raw  = _find(specs, "mainboard support", "hỗ trợ mainboard", "form factor", "kích thước mainboard")
            ff      = normalize_form_factor(ff_raw or "ATX")
            max_vga = parse_length_mm(_find(specs, "chiều dài vga tối đa", "max gpu length", "gpu length"))
            color   = (_find(specs, "màu sắc", "color") or "")[:30] or None
            case_type = normalize_case_type(_find(specs, "loại case", "kiểu case", "tower") or "", name)
            radiator  = parse_radiator_support(specs, name)

            results.append(CaseEnclosure(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                FormFactorSupport=ff,
                MaxVGALength=max_vga or 350,
                Color=color,
                CaseType=case_type,
                RadiatorSupport=radiator,
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} Cases")
    return results


# ── Storage ───────────────────────────────────────────────────────────────────────

def scrape_storage(max_pages: int = 10) -> list[Storage]:
    print("\n[Storage]")
    results = []
    for cat_key in ("ssd", "hdd"):
        for soup in get_pages(cat_key, max_pages):
            for card in get_cards(soup):
                basic = parse_card(card)
                if not basic["name"]:
                    continue

                specs  = get_specs(basic["url"])
                time.sleep(0.3)
                name   = basic["name"].lower()

                if "nvme" in name or "m.2" in name:
                    stor_type, interface = "NVMe", "M.2"
                elif cat_key == "ssd":
                    stor_type, interface = "SSD", "SATA"
                else:
                    stor_type, interface = "HDD", "SATA"

                capacity    = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or basic["name"])
                read_speed  = _int(_find(specs, "tốc độ đọc", "read speed", "đọc"))
                write_speed = _int(_find(specs, "tốc độ ghi", "write speed", "ghi"))

                results.append(Storage(
                    Name=basic["name"],
                    Manufacturer=extract_manufacturer_from_name(basic["name"]),
                    Price=basic["price"],
                    Type=stor_type,
                    Capacity=capacity,
                    Interface=interface,
                    ReadSpeed=read_speed,
                    WriteSpeed=write_speed,
                    ImageUrl=basic["image"],
                    Stock=1,
                ))
    print(f"  → {len(results)} Storage")
    return results


# ── CPU Cooler ────────────────────────────────────────────────────────────────────

def scrape_cpu_coolers(max_pages: int = 10) -> list[CpuCooler]:
    print("\n[CPU Cooler]")
    results = []
    for soup in get_pages("cooler", max_pages):
        for card in get_cards(soup):
            basic = parse_card(card)
            if not basic["name"]:
                continue

            specs  = get_specs(basic["url"])
            time.sleep(0.3)
            name   = basic["name"]

            socket_raw = _find(specs, "socket hỗ trợ", "compatible sockets", "socket", "tương thích socket")
            max_tdp    = parse_tdp_watts(_find(specs, "tdp tối đa", "max tdp", "tdp"))
            height     = parse_length_mm(_find(specs, "chiều cao", "height", "độ cao"))
            name_l     = name.lower()
            cooler_type = "AIO-360" if "360" in name_l else "AIO-240" if ("aio" in name_l or "240" in name_l) else "Air"

            results.append(CpuCooler(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=basic["price"],
                SocketCompatibility=(socket_raw or "Universal")[:200],
                MaxTDP=max_tdp or 150,
                Height=height or 160,
                Type=cooler_type,
                ImageUrl=basic["image"],
                Stock=1,
            ))
    print(f"  → {len(results)} Coolers")
    return results


# ── Helpers ───────────────────────────────────────────────────────────────────────

def _find(specs: dict, *keys) -> str:
    for k in keys:
        for sk in specs:
            if k in sk:
                return specs[sk]
    return ""


def _int(raw: str) -> int:
    m = re.search(r"\d+", str(raw))
    return int(m.group()) if m else 0


# Boost clock (GHz) by CPU model number substring (longest key first = most specific wins).
# Keys are lowercased with dashes/spaces removed for matching.
_CPU_BOOST_GHZ: list[tuple[str, float]] = sorted([
    # Intel Core Ultra (Arrow Lake 200-series)
    ("ultra9285k", 5.7), ("ultra7265k", 5.7), ("ultra5245k", 5.2),
    # Intel 14th gen
    ("i914900ks", 6.2), ("i914900kf", 6.0), ("i914900k", 6.0),
    ("i714700kf", 5.6), ("i714700k",  5.6), ("i714700f", 5.4), ("i714700", 5.4),
    ("i514600kf", 5.3), ("i514600k",  5.3), ("i514500",  5.0),
    ("i514400f",  4.7), ("i514400",   4.7),
    ("i314100f",  4.7), ("i314100",   4.7),
    # Intel 13th gen
    ("i913900ks", 6.0), ("i913900kf", 5.8), ("i913900k", 5.8),
    ("i713700kf", 5.4), ("i713700k",  5.4), ("i713700f", 5.2), ("i713700", 5.2),
    ("i513600kf", 5.1), ("i513600k",  5.1), ("i513500",  4.8),
    ("i513400f",  4.6), ("i513400",   4.6),
    ("i313100f",  4.5), ("i313100",   4.5),
    # Intel 12th gen
    ("i912900ks", 5.2), ("i912900kf", 5.2), ("i912900k", 5.2),
    ("i712700kf", 5.0), ("i712700k",  5.0), ("i712700f", 4.9), ("i712700", 4.9),
    ("i512600kf", 4.9), ("i512600k",  4.9), ("i512500",  4.6),
    ("i512400f",  4.4), ("i512400",   4.4),
    ("i312100f",  4.3), ("i312100",   4.3),
    # AMD Ryzen 9000 (Zen 5 / AM5)
    ("9950x", 5.7), ("9900x", 5.6), ("9700x", 5.5), ("9600x", 5.4),
    # AMD Ryzen 7000 (Zen 4 / AM5)
    ("7950x3d", 5.7), ("7950x", 5.7),
    ("7900x3d",  5.6), ("7900x", 5.6), ("7900", 5.4),
    ("7800x3d",  5.0),
    ("7700x",    5.4), ("7700", 5.3),
    ("7600x",    5.3), ("7600", 5.1), ("7500f", 5.0),
    # AMD Ryzen 5000 (Zen 3 / AM4)
    ("5950x",   4.9),
    ("5900x",   4.8),
    ("5800x3d", 4.5), ("5800x", 4.7), ("5800", 4.6),
    ("5700x",   4.6), ("5700g", 4.6), ("5700", 4.6),
    ("5600x",   4.6), ("5600g", 4.4), ("5600", 4.4), ("5500", 4.2),
    # AMD Ryzen 3000 (Zen 2 / AM4)
    ("3950x",  4.7),
    ("3900xt", 4.7), ("3900x", 4.6),
    ("3800xt", 4.7), ("3800x", 4.5),
    ("3700x",  4.4),
    ("3600xt", 4.5), ("3600x", 4.4), ("3600", 4.2),
    ("3300x",  4.3), ("3100",  3.6),
], key=lambda x: -len(x[0]))  # longest key first → most specific match wins


def _boost_clock_from_name(name: str) -> float:
    """Extract boost/max clock from product name string."""
    name_l = name.lower()

    # 1. Explicit boost/turbo keyword labels
    for pat in [
        r"boost\s+(?:tối\s+đa\s+)?([\d.]+)\s*ghz",
        r"up\s+to\s+([\d.]+)\s*ghz",
        r"tối\s+đa\s+([\d.]+)\s*ghz",
        r"turbo\s+([\d.]+)\s*ghz",
        r"([\d.]+)\s*ghz\s+turbo",
    ]:
        m = re.search(pat, name_l)
        if m:
            val = float(m.group(1))
            if 1.0 < val < 7.0:
                return round(val, 2)

    # 2. Range "X.X GHz - Y.Y GHz" or "X.X/Y.Y GHz" → take max (= boost)
    m = re.search(r"([\d.]+)\s*ghz\s*[-/]\s*([\d.]+)\s*ghz", name_l)
    if m:
        best = max(float(m.group(1)), float(m.group(2)))
        if 1.0 < best < 7.0:
            return round(best, 2)

    # 3. Model number lookup table (covers bare names like "Intel Core i5-12400F")
    key = re.sub(r"[\s\-]", "", name_l)
    for model, boost in _CPU_BOOST_GHZ:
        if model in key:
            return boost

    # 4. Last resort: any GHz value in range
    m = re.search(r"([\d.]+)\s*ghz", name_l)
    if m:
        val = float(m.group(1))
        if 1.0 < val < 7.0:
            return round(val, 2)

    return 0.0


def _socket_from_name(name: str) -> str:
    name_u = name.upper()
    for s in ["LGA1700", "LGA1200", "LGA1151", "AM5", "AM4", "AM3+"]:
        if s in name_u or s.replace("LGA", "LGA ") in name_u:
            return s
    if "1700" in name_u: return "LGA1700"
    if "1200" in name_u: return "LGA1200"
    if "AM5"  in name_u: return "AM5"
    if "AM4"  in name_u: return "AM4"
    return ""


# Chipset → socket map for motherboard name parsing
_MB_CHIPSET_SOCKET: list[tuple[list[str], str]] = [
    # Intel LGA1700 (12th/13th/14th gen)
    (["Z790", "B760", "H770", "H610", "Z690", "B660", "H670", "W790", "W680"], "LGA1700"),
    # Intel LGA1200 (10th/11th gen)
    (["Z590", "H570", "B560", "H510", "Z490", "B460", "H410"], "LGA1200"),
    # Intel LGA1151 (6th–9th gen)
    (["Z390", "B365", "H370", "B360", "H310", "Z370", "Z270", "B250", "H270", "Z170", "B150", "H110"], "LGA1151"),
    # AMD AM5 (Zen 4+)
    (["X870E", "X870", "B850", "B840", "X670E", "X670", "B650E", "B650", "A620"], "AM5"),
    # AMD AM4 (Zen 1–3)
    (["X570", "B550", "X470", "B450", "X370", "B350", "A320", "A300"], "AM4"),
]


def _mb_socket_from_name(name: str) -> str:
    """Infer motherboard socket from chipset model in product name."""
    name_u = name.upper()
    # Direct socket mentions first
    socket = _socket_from_name(name)
    if socket:
        return socket
    # Chipset pattern: word boundary + chipset code (e.g. "B760M", "Z790-P")
    for chipsets, sock in _MB_CHIPSET_SOCKET:
        for chip in chipsets:
            if re.search(r'\b' + chip + r'\b', name_u):
                return sock
    return ""


def _cores_from_name(name: str) -> int:
    m = re.search(r"(\d+)\s*(?:nhân|core|lõi)", name.lower())
    return int(m.group(1)) if m else 0


def _vram_from_name(name: str) -> int:
    """Extract VRAM GB from GPU name: '8G', '16GB', '12G GDDR6X', '96G' etc.
    Avoids false-positives by requiring a realistic VRAM value (1-96 GB)."""
    name_u = name.upper()
    # Match "8GB", "16GB", "8G " (space/end), "8G GDDR", "96G", but NOT "5060G" (model number)
    m = re.search(r'\b(\d{1,3})\s*G(?:B|DDR|\s|$)', name_u)
    if m:
        val = int(m.group(1))
        if 1 <= val <= 96:
            return val
    return 0


# CPU TDP lookup: model substring → TDP in watts (longest match wins)
_CPU_TDP_MAP: list[tuple[str, int]] = sorted([
    # Intel 14th gen
    ("i9-14900ks", 253), ("i9-14900k", 125), ("i9-14900kf", 125), ("i9-14900f", 65), ("i9-14900", 65),
    ("i7-14700k", 125), ("i7-14700kf", 125), ("i7-14700f", 65), ("i7-14700", 65),
    ("i5-14600k", 125), ("i5-14600kf", 125), ("i5-14500", 65), ("i5-14400f", 65), ("i5-14400", 65),
    ("i3-14100f", 58), ("i3-14100", 58),
    # Intel 13th gen
    ("i9-13900ks", 253), ("i9-13900k", 125), ("i9-13900kf", 125), ("i9-13900f", 65), ("i9-13900", 65),
    ("i7-13700k", 125), ("i7-13700kf", 125), ("i7-13700f", 65), ("i7-13700", 65),
    ("i5-13600k", 125), ("i5-13600kf", 125), ("i5-13500", 65), ("i5-13400f", 65), ("i5-13400", 65),
    ("i3-13100f", 58), ("i3-13100", 58),
    # Intel 12th gen
    ("i9-12900ks", 150), ("i9-12900k", 125), ("i9-12900kf", 125), ("i9-12900f", 65), ("i9-12900", 65),
    ("i7-12700k", 125), ("i7-12700kf", 125), ("i7-12700f", 65), ("i7-12700", 65),
    ("i5-12600k", 125), ("i5-12600kf", 125), ("i5-12500", 65), ("i5-12400f", 65), ("i5-12400", 65),
    ("i3-12100f", 58), ("i3-12100", 58),
    # AMD Ryzen 7000
    ("ryzen 9 7950x3d", 120), ("ryzen 9 7950x", 170), ("ryzen 9 7900x3d", 120), ("ryzen 9 7900x", 170),
    ("ryzen 9 7900", 65), ("ryzen 7 7800x3d", 120), ("ryzen 7 7700x", 105), ("ryzen 7 7700", 65),
    ("ryzen 5 7600x", 105), ("ryzen 5 7600", 65), ("ryzen 5 7500f", 65),
    # AMD Ryzen 5000
    ("ryzen 9 5950x", 105), ("ryzen 9 5900x", 105), ("ryzen 9 5900", 65),
    ("ryzen 7 5800x3d", 105), ("ryzen 7 5800x", 105), ("ryzen 7 5800", 65),
    ("ryzen 7 5700x", 65), ("ryzen 7 5700g", 65), ("ryzen 5 5600x", 65),
    ("ryzen 5 5600g", 65), ("ryzen 5 5600", 65), ("ryzen 3 5300g", 65),
    # AMD Ryzen 3000
    ("ryzen 9 3950x", 105), ("ryzen 9 3900xt", 105), ("ryzen 9 3900x", 105),
    ("ryzen 7 3800xt", 105), ("ryzen 7 3800x", 105), ("ryzen 7 3700x", 65),
    ("ryzen 5 3600x", 95), ("ryzen 5 3600", 65), ("ryzen 3 3300x", 65),
    # AMD Ryzen 9000 (Zen 5)
    ("ryzen 9 9950x3d", 170), ("ryzen 9 9950x", 170), ("ryzen 9 9900x3d", 120), ("ryzen 9 9900x", 120),
    ("ryzen 9 9900", 65), ("ryzen 7 9800x3d", 120), ("ryzen 7 9700x", 65), ("ryzen 7 9700", 65),
    ("ryzen 5 9600x", 65), ("ryzen 5 9600", 65), ("ryzen 3 9300", 65),
    # Intel Core Ultra (Arrow Lake — LGA1851)
    ("core ultra 9 285k", 125), ("core ultra 7 265k", 125), ("core ultra 7 265kf", 125),
    ("core ultra 5 245k", 125), ("core ultra 5 245kf", 125),
    ("core ultra 9 285", 65), ("core ultra 7 265", 65), ("core ultra 5 245", 65),
    ("core ultra 5 235", 65), ("core ultra 5 225", 65),
    # Intel Core Ultra (Meteor Lake — U/H series, low power)
    ("core ultra 9 185h", 45), ("core ultra 7 165h", 45), ("core ultra 5 125h", 28),
    # AMD Threadripper
    ("threadripper 7980x", 350), ("threadripper 7970x", 350), ("threadripper 7960x", 350),
    ("threadripper pro 7995wx", 350), ("threadripper pro 7985wx", 350),
    ("threadripper pro 7975wx", 350), ("threadripper pro 7965wx", 350),
    # AMD Ryzen 2000/1000
    ("ryzen 7 2700x", 105), ("ryzen 7 2700", 65), ("ryzen 5 2600x", 95), ("ryzen 5 2600", 65),
    ("ryzen 5 1600x", 95), ("ryzen 5 1600", 65),
    # AMD Ryzen G/APU series
    ("ryzen 7 5700g", 65), ("ryzen 5 5600g", 65), ("ryzen 3 4300g", 65),
    ("ryzen 3 3200g", 65), ("ryzen 5 3400g", 65),
    ("ryzen 5 8500g", 65), ("ryzen 5 8600g", 65), ("ryzen 7 8700g", 65),
    ("ryzen 5 5500gt", 65), ("ryzen 5 5600gt", 65),
    # AMD Ryzen 9000 additional
    ("ryzen 7 9850x3d", 120),
    # AMD Ryzen older
    ("ryzen 7 1800x", 95), ("ryzen 7 1700x", 95), ("ryzen 7 1700", 65),
    ("ryzen 5 3500", 65), ("ryzen 3 3100", 65), ("ryzen 3 3300x", 65),
    # Intel Core Ultra 200K (Arrow Lake, new)
    ("core ultra 7 270k", 125), ("core ultra 7 270kf", 125),
], key=lambda x: -len(x[0]))  # longest match first


def _tdp_from_name(name: str) -> int:
    """Look up CPU TDP from product name. Longest substring match wins."""
    # Strip trademark symbols and normalize spacing around model numbers
    name_l = re.sub(r"[™®©]", "", name.lower())
    # Normalize "core i7 14700k" → "i7-14700k" (space → dash)
    name_l = re.sub(r"core\s+i(\d)\s+(\d{4,5})", r"i\1-\2", name_l)
    # Normalize "core ultra 5-225k" → "core ultra 5 225k" (dash → space for Ultra series)
    name_l = re.sub(r"(ultra\s+\d)-(\d)", r"\1 \2", name_l)
    # Normalize "ryzen9 " → "ryzen 9 " (no-space variant)
    name_l = re.sub(r"ryzen(\d)", r"ryzen \1", name_l)
    for model, tdp in _CPU_TDP_MAP:
        if model in name_l:
            return tdp
    return 0
