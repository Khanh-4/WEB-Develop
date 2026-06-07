"""
TTGShop scraper — https://ttgshop.vn
Static HTML pages with div.p-item cards.
Pagination: ?page=N, stop when no cards found.
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
from scrapers.phongvu import (
    _socket_from_name, _mb_socket_from_name, _boost_clock_from_name,
    _cores_from_name, _vram_from_name, _tdp_from_name,
)

BASE    = "https://ttgshop.vn"
HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

CATEGORY_URLS = {
    "cpu":         BASE + "/cpu?page={page}",
    "motherboard": BASE + "/mainboard-1?page={page}",
    "memory":      BASE + "/ram?page={page}",
    "gpu":         BASE + "/vga-card-man-hinh?page={page}",
    "psu":         BASE + "/nguon?page={page}",
    "case":        BASE + "/vo-case?page={page}",
    "ssd":         BASE + "/o-cung-ssd?page={page}",
    "hdd":         BASE + "/o-cung-hdd?page={page}",
    "cooler":      BASE + "/tan-nhiet-cpu?page={page}",
}


def _get_soup(url: str, retries: int = 3) -> BeautifulSoup | None:
    for attempt in range(retries):
        try:
            r = requests.get(url, headers=HEADERS, timeout=15)
            r.raise_for_status()
            return BeautifulSoup(r.text, "lxml")
        except Exception as e:
            print(f"  [retry {attempt+1}/{retries}] {url[:60]} — {e}")
            time.sleep(2 ** attempt)
    return None


def _get_pages(category_key: str, max_pages: int = 10) -> list[BeautifulSoup]:
    pages = []
    template = CATEGORY_URLS[category_key]
    for page in range(1, max_pages + 1):
        soup = _get_soup(template.format(page=page))
        if soup is None:
            break
        cards = soup.select("div.p-item")
        if not cards:
            break
        pages.append(soup)
        print(f"  page {page}: {len(cards)} cards")
        time.sleep(0.6)
    return pages


def _parse_card(card: BeautifulSoup) -> dict:
    name_el  = card.select_one("h3.p-name, .p-name")
    price_el = card.select_one("p.p-price-sale, .p-price-sale")
    img_el   = card.select_one("img")
    link_el  = card.select_one("a[href]")

    name  = name_el.get_text(strip=True) if name_el else ""
    # Also try the link title if name is truncated
    if not name and link_el:
        name = link_el.get("title", "").strip()

    price = parse_price(price_el.get_text(strip=True)) if price_el else 0
    image = img_el.get("data-src") or img_el.get("src") if img_el else None
    if image and not image.startswith("http"):
        image = BASE + image
    href  = link_el["href"] if link_el else None
    url   = (href if href and href.startswith("http") else BASE + href) if href else None

    return {"name": name, "price": price, "image": image, "url": url}


def _get_specs(product_url: str) -> dict[str, str]:
    if not product_url:
        return {}
    soup = _get_soup(product_url)
    if not soup:
        return {}
    specs: dict[str, str] = {}
    for row in soup.select("table tr"):
        cells = row.select("td, th")
        if len(cells) >= 2:
            k = cells[0].get_text(strip=True).lower().strip(":")
            v = cells[1].get_text(strip=True)
            if k and v:
                specs[k] = v
    return specs


def _find(specs: dict, *keys) -> str:
    for k in keys:
        for sk in specs:
            if k in sk:
                return specs[sk]
    return ""


def _int(raw: str) -> int:
    m = re.search(r"\d+", str(raw))
    return int(m.group()) if m else 0


# ── CPU ────────────────────────────────────────────────────────────────────────

def scrape_cpus(max_pages: int = 10) -> list[Cpu]:
    print("\n[TTG CPU]")
    results = []
    for soup in _get_pages("cpu", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            socket  = normalize_socket(_find(specs, "socket", "loại socket"))
            cores   = _int(_find(specs, "số nhân", "cores", "nhân"))
            threads = _int(_find(specs, "số luồng", "threads", "luồng"))
            base_c  = parse_clock_ghz(_find(specs, "xung nhịp cơ bản", "base clock", "tốc độ cơ bản"))
            boost_c = parse_clock_ghz(_find(specs, "xung nhịp turbo", "boost", "turbo", "tốc độ tối đa"))
            tdp     = parse_tdp_watts(_find(specs, "tdp", "công suất", "tiêu thụ"))

            if not socket: socket = _mb_socket_from_name(name) or _socket_from_name(name)
            if cores == 0: cores = _cores_from_name(name)
            if boost_c == 0: boost_c = _boost_clock_from_name(name)
            if base_c == 0 and boost_c > 0: base_c = round(boost_c * 0.85, 2)
            if tdp == 0: tdp = _tdp_from_name(name)

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
    print("\n[TTG Motherboard]")
    results = []
    for soup in _get_pages("motherboard", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            socket  = normalize_socket(_find(specs, "socket", "loại socket", "cpu socket"))
            ff      = normalize_form_factor(_find(specs, "form factor", "kích thước", "chuẩn"))
            memtype = normalize_memory_type(_find(specs, "loại ram", "memory type", "chuẩn ram") or name)
            slots   = _int(_find(specs, "số khe ram", "memory slots")) or 4
            chipset = normalize_chipset(_find(specs, "chipset", "chip", "vi điều khiển"), name)

            if not socket: socket = _mb_socket_from_name(name)

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
    print("\n[TTG Memory]")
    results = []
    for soup in _get_pages("memory", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            memtype  = normalize_memory_type(_find(specs, "loại ram", "type", "chuẩn") or name)
            capacity = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or name)
            speed    = parse_speed_mhz(_find(specs, "tốc độ", "speed", "bus", "xung") or name)
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
    print("\n[TTG GPU]")
    results = []
    for soup in _get_pages("gpu", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            vram_spec = _find(specs, "bộ nhớ", "vram", "memory", "dung lượng")
            vram   = parse_capacity_gb(vram_spec) if vram_spec else _vram_from_name(name)
            length = parse_length_mm(_find(specs, "chiều dài", "card length", "length"))
            tdp    = parse_tdp_watts(_find(specs, "tdp", "công suất"))

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
    print("\n[TTG PSU]")
    results = []
    for soup in _get_pages("psu", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

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
    print("\n[TTG Case]")
    results = []
    for soup in _get_pages("case", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            ff_raw  = _find(specs, "mainboard support", "hỗ trợ mainboard", "form factor", "kích thước mainboard")
            ff      = normalize_form_factor(ff_raw or "ATX")
            max_vga = parse_length_mm(_find(specs, "chiều dài vga", "max gpu", "gpu length", "chiều dài card"))
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
    print("\n[TTG Storage]")
    results = []
    for cat_key in ("ssd", "hdd"):
        for soup in _get_pages(cat_key, max_pages):
            for card in soup.select("div.p-item"):
                basic = _parse_card(card)
                if not basic["name"]:
                    continue
                specs = _get_specs(basic["url"])
                time.sleep(0.3)
                name  = basic["name"].lower()

                if "nvme" in name or "m.2" in name:
                    stor_type, interface = "NVMe", "M.2"
                elif cat_key == "ssd":
                    stor_type, interface = "SSD", "SATA"
                else:
                    stor_type, interface = "HDD", "SATA"

                capacity    = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or basic["name"])
                read_speed  = _int(_find(specs, "tốc độ đọc", "read speed"))
                write_speed = _int(_find(specs, "tốc độ ghi", "write speed"))

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
    print("\n[TTG Cooler]")
    results = []
    for soup in _get_pages("cooler", max_pages):
        for card in soup.select("div.p-item"):
            basic = _parse_card(card)
            if not basic["name"]:
                continue
            specs = _get_specs(basic["url"])
            time.sleep(0.3)
            name = basic["name"]

            socket_raw = _find(specs, "socket hỗ trợ", "compatible sockets", "socket", "tương thích")
            max_tdp    = parse_tdp_watts(_find(specs, "tdp tối đa", "max tdp", "tdp"))
            height     = parse_length_mm(_find(specs, "chiều cao", "height"))
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
