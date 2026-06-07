"""
GearVN scraper — https://gearvn.com (Haravan platform)
Collection pages are JS-rendered; product detail pages are static HTML.
Strategy: parse product sitemaps → filter by category URL keywords → scrape detail pages.
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

BASE    = "https://gearvn.com"
HEADERS = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

# URL keyword filters per category (matched against product slug)
_CAT_KEYWORDS: dict[str, list[str]] = {
    "cpu":         ["bo-vi-xu-ly", "cpu-intel", "cpu-amd", "core-i3", "core-i5", "core-i7", "core-i9",
                    "core-ultra", "ryzen-3", "ryzen-5", "ryzen-7", "ryzen-9"],
    "motherboard": ["mainboard-", "bo-mach-chu"],
    "memory":      ["/ram-", "-ram-"],
    "gpu":         ["card-man-hinh", "vga-msi", "vga-asus", "vga-gigabyte", "vga-zotac", "vga-gainward",
                    "vga-palit", "vga-galax", "vga-inno3d", "vga-pny", "vga-colorful",
                    "rtx-", "gtx-", "/rx-"],
    "psu":         ["nguon-may-tinh", "nguon-psu", "psu-"],
    "case":        ["vo-case-", "thung-may-tinh"],
    "ssd":         ["o-cung-ssd", "ssd-"],
    "hdd":         ["o-cung-hdd", "hdd-", "hard-disk"],
    "cooler":      ["tan-nhiet-cpu", "tan-nhiet-khi", "tan-nhiet-nuoc", "cpu-cooler"],
}

_SITEMAP_COUNT = 5


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


def _get_all_product_urls() -> list[str]:
    """Fetch all product URLs from GearVN sitemaps (cached across calls)."""
    urls = []
    for n in range(1, _SITEMAP_COUNT + 1):
        soup = _get_soup(f"{BASE}/sitemap_products_{n}.xml")
        if not soup:
            continue
        batch = [loc.get_text(strip=True)
                 for loc in soup.find_all("loc")
                 if "/products/" in loc.get_text()]
        urls.extend(batch)
        print(f"  sitemap_{n}: {len(batch)} URLs")
    print(f"  Total product URLs: {len(urls)}")
    return urls


_PREBUILT_EXCLUDE = ["pc-gvn", "/pc-", "may-tinh-bo", "bo-may-tinh", "pc-gaming"]

def _filter_urls(all_urls: list[str], category: str) -> list[str]:
    keywords = _CAT_KEYWORDS.get(category, [])
    return [
        u for u in all_urls
        if any(kw in u for kw in keywords)
        and not any(ex in u for ex in _PREBUILT_EXCLUDE)
    ]


def _get_specs(product_url: str) -> tuple[str, str | None, int, dict[str, str]]:
    """Returns (name, image_url, price, specs_dict).

    GearVN (Haravan) embeds product data in window.shop.product JS.
    Price is stored ×100 (e.g. 1099000000 = 10,990,000 VND).
    Spec tables exist on some product pages; fall back to name-based parsing when absent.
    """
    import re as _re
    soup = _get_soup(product_url)
    if not soup:
        return "", None, 0, {}

    # Name
    name_el = soup.select_one("h1")
    name = name_el.get_text(strip=True) if name_el else ""

    price = 0
    image = None
    specs: dict[str, str] = {}

    for s in soup.find_all("script"):
        txt = s.string or ""
        if "price_min" not in txt:
            continue

        # Price: price_min is stored ×100 (Haravan cents)
        pm = _re.search(r'"price_min"\s*:\s*([\d.]+)', txt)
        if pm:
            price = int(float(pm.group(1)) / 100)

        # Image: first cdn.hstatic.net/products URL (collapse line-continuations first)
        clean = _re.sub(r'\\\n\s*', '', txt)
        imgs = _re.findall(r'https?://cdn\.hstatic\.net/products/\d+/[^"\'\s\\]+\.jpg', clean)
        if imgs:
            image = imgs[0]

        # Specs: parse description HTML for tables
        desc_m = _re.search(r'"description"\s*:\s*"(.*?)",\s*"(?:handle|id|available)"', txt, re.DOTALL)
        if desc_m:
            raw_html = desc_m.group(1).replace('\\n', '').replace('\\"', '"').replace('\\/', '/')
            desc_soup = BeautifulSoup(raw_html, "lxml")
            for row in desc_soup.select("tr"):
                cells = row.select("td, th")
                if len(cells) >= 2:
                    k = cells[0].get_text(strip=True).lower().strip(":")
                    v = cells[1].get_text(strip=True)
                    if k and v:
                        specs[k] = v
        break

    return name, image, price, specs


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

def scrape_cpus(all_urls: list[str]) -> list[Cpu]:
    print("\n[GearVN CPU]")
    results = []
    urls = _filter_urls(all_urls, "cpu")
    print(f"  {len(urls)} CPU URLs found")
    for url in urls:
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

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
            Price=price,
            Socket=socket or "Unknown",
            CoreCount=cores,
            ThreadCount=threads or cores * 2,
            BaseClock=base_c,
            BoostClock=boost_c,
            TDP=tdp,
            ApproximatePerformance=score_cpu(cores, base_c, boost_c, tdp),
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} CPUs")
    return results


# ── Motherboard ─────────────────────────────────────────────────────────────────

def scrape_motherboards(all_urls: list[str]) -> list[Motherboard]:
    print("\n[GearVN Motherboard]")
    results = []
    for url in _filter_urls(all_urls, "motherboard"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        socket  = normalize_socket(_find(specs, "socket", "loại socket", "cpu socket"))
        ff      = normalize_form_factor(_find(specs, "form factor", "kích thước", "chuẩn"))
        memtype = normalize_memory_type(_find(specs, "loại ram", "memory type", "chuẩn ram") or name)
        slots   = _int(_find(specs, "số khe ram", "memory slots")) or 4
        chipset = normalize_chipset(_find(specs, "chipset", "chip", "vi điều khiển"), name)

        if not socket: socket = _mb_socket_from_name(name)

        results.append(Motherboard(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            SocketCompatibility=socket or "Unknown",
            FormFactor=ff,
            MemoryCompatibility=memtype,
            MemorySlots=slots,
            MaxMemoryCapacity=128,
            Chipset=chipset,
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} Motherboards")
    return results


# ── Memory ──────────────────────────────────────────────────────────────────────

def scrape_memory(all_urls: list[str]) -> list[Memory]:
    print("\n[GearVN Memory]")
    results = []
    for url in _filter_urls(all_urls, "memory"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        memtype  = normalize_memory_type(_find(specs, "loại ram", "type", "chuẩn") or name)
        capacity = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or name)
        speed    = parse_speed_mhz(_find(specs, "tốc độ", "speed", "bus", "xung") or name)
        modules  = _int(_find(specs, "số thanh", "kit")) or 1
        profile = normalize_ram_profile(_find(specs, "profile", "xmp", "expo") or "", name)

        results.append(Memory(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            Type=memtype,
            Capacity=capacity,
            Modules=modules,
            Speed=speed,
            Profile=profile,
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} Memory")
    return results


# ── GPU ──────────────────────────────────────────────────────────────────────────

def scrape_video_cards(all_urls: list[str]) -> list[VideoCard]:
    print("\n[GearVN GPU]")
    results = []
    for url in _filter_urls(all_urls, "gpu"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        vram_spec = _find(specs, "bộ nhớ", "vram", "memory", "dung lượng")
        vram   = parse_capacity_gb(vram_spec) if vram_spec else _vram_from_name(name)
        length = parse_length_mm(_find(specs, "chiều dài", "card length", "length"))
        tdp    = parse_tdp_watts(_find(specs, "tdp", "công suất"))

        results.append(VideoCard(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            VRAM=vram,
            Length=length or 280,
            TDP=tdp,
            ApproximatePerformance=score_gpu(name, vram, tdp),
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} GPUs")
    return results


# ── PSU ──────────────────────────────────────────────────────────────────────────

def scrape_power_supplies(all_urls: list[str]) -> list[PowerSupply]:
    print("\n[GearVN PSU]")
    results = []
    for url in _filter_urls(all_urls, "psu"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        wattage    = parse_wattage(_find(specs, "công suất", "wattage") or name)
        efficiency = normalize_efficiency(_find(specs, "hiệu suất", "efficiency", "chứng nhận") or "")
        modular    = (_find(specs, "modular", "dạng cáp") or "Non")[:20]
        psu_ff = normalize_psu_form_factor(_find(specs, "form factor", "kích thước nguồn") or "", name)

        results.append(PowerSupply(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            Wattage=wattage,
            Efficiency=efficiency,
            Modular=modular,
            PsuFormFactor=psu_ff,
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} PSUs")
    return results


# ── Case ─────────────────────────────────────────────────────────────────────────

def scrape_cases(all_urls: list[str]) -> list[CaseEnclosure]:
    print("\n[GearVN Case]")
    results = []
    for url in _filter_urls(all_urls, "case"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        ff_raw  = _find(specs, "mainboard support", "hỗ trợ mainboard", "form factor", "kích thước mainboard")
        ff      = normalize_form_factor(ff_raw or "ATX")
        max_vga = parse_length_mm(_find(specs, "chiều dài vga", "max gpu", "gpu length", "chiều dài card"))
        color   = (_find(specs, "màu sắc", "color") or "")[:30] or None
        case_type = normalize_case_type(_find(specs, "loại case", "kiểu case", "tower") or "", name)
        radiator  = parse_radiator_support(specs, name)

        results.append(CaseEnclosure(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            FormFactorSupport=ff,
            MaxVGALength=max_vga or 350,
            Color=color,
            CaseType=case_type,
            RadiatorSupport=radiator,
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} Cases")
    return results


# ── Storage ───────────────────────────────────────────────────────────────────────

def scrape_storage(all_urls: list[str]) -> list[Storage]:
    print("\n[GearVN Storage]")
    results = []
    for cat in ("ssd", "hdd"):
        for url in _filter_urls(all_urls, cat):
            name, image, price, specs = _get_specs(url)
            if not name or price == 0:
                continue
            time.sleep(0.3)
            name_l = name.lower()

            if "nvme" in name_l or "m.2" in name_l:
                stor_type, interface = "NVMe", "M.2"
            elif cat == "ssd":
                stor_type, interface = "SSD", "SATA"
            else:
                stor_type, interface = "HDD", "SATA"

            capacity    = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or name)
            read_speed  = _int(_find(specs, "tốc độ đọc", "read speed"))
            write_speed = _int(_find(specs, "tốc độ ghi", "write speed"))

            results.append(Storage(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=price,
                Type=stor_type,
                Capacity=capacity,
                Interface=interface,
                ReadSpeed=read_speed,
                WriteSpeed=write_speed,
                ImageUrl=image,
                Stock=1,
            ))
    print(f"  → {len(results)} Storage")
    return results


# ── CPU Cooler ────────────────────────────────────────────────────────────────────

def scrape_cpu_coolers(all_urls: list[str]) -> list[CpuCooler]:
    print("\n[GearVN Cooler]")
    results = []
    for url in _filter_urls(all_urls, "cooler"):
        name, image, price, specs = _get_specs(url)
        if not name or price == 0:
            continue
        time.sleep(0.3)

        socket_raw = _find(specs, "socket hỗ trợ", "compatible sockets", "socket", "tương thích")
        max_tdp    = parse_tdp_watts(_find(specs, "tdp tối đa", "max tdp", "tdp"))
        height     = parse_length_mm(_find(specs, "chiều cao", "height"))
        name_l     = name.lower()
        cooler_type = "AIO-360" if "360" in name_l else "AIO-240" if ("aio" in name_l or "240" in name_l) else "Air"

        results.append(CpuCooler(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            SocketCompatibility=(socket_raw or "Universal")[:200],
            MaxTDP=max_tdp or 150,
            Height=height or 160,
            Type=cooler_type,
            ImageUrl=image,
            Stock=1,
        ))
    print(f"  → {len(results)} Coolers")
    return results
