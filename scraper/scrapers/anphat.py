"""
An Phát Computer scraper — https://www.anphatpc.com.vn
Products are loaded via JSON API (not static HTML).
API: /ajax/get_json.php?action=product&action_type=product-list&category={id}&show={n}&page={p}
Pagination: keep fetching until collected == total.
"""

import re
import time
import requests

from processors.normalizer import (
    parse_capacity_gb, parse_speed_mhz, parse_clock_ghz,
    parse_tdp_watts, parse_length_mm, parse_wattage,
    normalize_socket, normalize_memory_type, normalize_form_factor,
    normalize_efficiency, extract_manufacturer_from_name,
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

BASE    = "https://www.anphatpc.com.vn"
API_URL = BASE + "/ajax/get_json.php"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "Referer": BASE + "/",
}
PAGE_SIZE = 20


# ── API helpers ─────────────────────────────────────────────────────────────────

def _fetch_page(cat_id: int, page: int) -> dict:
    for attempt in range(3):
        try:
            r = requests.get(
                API_URL,
                params={
                    "action": "product",
                    "action_type": "product-list",
                    "category": cat_id,
                    "show": PAGE_SIZE,
                    "page": page,
                    "sort": "order",
                },
                headers=HEADERS,
                timeout=15,
            )
            r.raise_for_status()
            return r.json()
        except Exception as e:
            print(f"  [retry {attempt+1}/3] cat={cat_id} page={page} — {e}")
            time.sleep(2 ** attempt)
    return {}


def _get_all_products(cat_ids: list[int]) -> list[dict]:
    """Fetch all products across multiple category IDs, deduplicated by productUrl."""
    seen: set[str] = set()
    results: list[dict] = []

    for cat_id in cat_ids:
        page = 1
        total = None
        collected = 0

        while True:
            data = _fetch_page(cat_id, page)
            items = data.get("list", [])
            if total is None:
                total = data.get("total", 0)

            for item in items:
                url = item.get("productUrl", "")
                if url and url not in seen:
                    seen.add(url)
                    results.append(item)
                    collected += 1

            print(f"  cat={cat_id} page={page}: {len(items)} items (total={total})")
            page += 1
            time.sleep(0.4)

            if not items or (total and collected >= total):
                break

    return results


def _parse_summary(summary: str) -> dict[str, str]:
    """Parse tab- or colon-separated key-value pairs from productSummary."""
    specs: dict[str, str] = {}
    for line in re.split(r"[\n\r]+", summary or ""):
        line = line.strip()
        if not line:
            continue
        if "\t" in line:
            parts = line.split("\t", 1)
        elif ": " in line:
            parts = line.split(": ", 1)
        elif ":" in line:
            parts = line.split(":", 1)
        else:
            continue
        if len(parts) == 2:
            k = parts[0].strip().lower().rstrip(":")
            v = parts[1].strip()
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


def _extract_socket(raw: str) -> str:
    """Extract socket token (AM5, AM4, LGA1700, etc.) from a verbose spec string."""
    m = re.search(r"\b(AM[45]|LGA\s*\d+)\b", raw, re.I)
    return m.group(0).replace(" ", "") if m else ""


def _ddr_type_or_name(spec_val: str, name: str) -> str:
    """Return spec_val if it contains DDR, else fall back to name for memory type extraction."""
    return spec_val if re.search(r"DDR", spec_val, re.I) else name


def _image(item: dict) -> str | None:
    img = item.get("productImage", {})
    url = img.get("large") or img.get("medium") or img.get("small") or ""
    return url if url else None


# ── CPU ─────────────────────────────────────────────────────────────────────────

def scrape_cpus() -> list[Cpu]:
    print("\n[AnPhat CPU]")
    results = []
    for item in _get_all_products([1025]):
        name = item.get("productName", "").strip()
        if not name or "server" in name.lower():
            continue
        price  = int(item.get("price", 0))
        specs  = _parse_summary(item.get("productSummary", ""))

        # Cores / threads — "Số lõi: 4 / Số luồng: 8"
        cores_raw = _find(specs, "số lõi", "cores")
        m_cores = re.search(r"(\d+)\s*/\s*(?:Số luồng|luồng|threads?)[\s:]*(\d+)", cores_raw, re.I)
        cores   = int(m_cores.group(1)) if m_cores else _cores_from_name(name)
        threads = int(m_cores.group(2)) if m_cores else cores * 2

        base_c  = parse_clock_ghz(_find(specs, "tần số cơ sở", "base clock", "tốc độ cơ bản"))
        boost_c = parse_clock_ghz(_find(specs, "tần số turbo", "boost", "turbo"))
        tdp     = parse_tdp_watts(_find(specs, "công suất", "tdp", "tiêu thụ"))
        socket  = normalize_socket(_find(specs, "hỗ trợ socket", "socket"))

        if not socket: socket = _mb_socket_from_name(name) or _socket_from_name(name)
        socket = socket[:50]
        if boost_c == 0: boost_c = _boost_clock_from_name(name)
        if base_c == 0 and boost_c > 0: base_c = round(boost_c * 0.85, 2)
        if tdp == 0: tdp = _tdp_from_name(name)

        results.append(Cpu(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            Socket=socket or "Unknown",
            CoreCount=cores,
            ThreadCount=threads,
            BaseClock=base_c,
            BoostClock=boost_c,
            TDP=tdp,
            ApproximatePerformance=score_cpu(cores, base_c, boost_c, tdp),
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} CPUs")
    return results


# ── GPU ─────────────────────────────────────────────────────────────────────────

def scrape_video_cards() -> list[VideoCard]:
    print("\n[AnPhat GPU]")
    results = []
    for item in _get_all_products([1155]):
        name = item.get("productName", "").strip()
        if not name or "server" in name.lower():
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        vram_raw = _find(specs, "dung lượng bộ nhớ", "dung lượng", "memory", "vram")
        vram     = parse_capacity_gb(vram_raw) if vram_raw else _vram_from_name(name)
        length   = parse_length_mm(_find(specs, "chiều dài", "card length", "length"))
        # "nguồn yêu cầu" is required PSU wattage, NOT GPU TDP — exclude it
        tdp_raw  = _find(specs, "power consumption", "tiêu thụ điện", "tdp tối đa", "tdp")
        tdp      = parse_tdp_watts(tdp_raw)

        results.append(VideoCard(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            VRAM=vram,
            Length=length or 280,
            TDP=tdp,
            ApproximatePerformance=score_gpu(name, vram, tdp),
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} GPUs")
    return results


# ── RAM ─────────────────────────────────────────────────────────────────────────

def scrape_memory() -> list[Memory]:
    print("\n[AnPhat RAM]")
    results = []
    for item in _get_all_products([1234]):
        name = item.get("productName", "").strip()
        # Skip server/ECC RAM
        if not name or re.search(r"\b(server|ecc|rdimm|udimm lrdimm)\b", name, re.I):
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        memtype  = normalize_memory_type(_find(specs, "loại", "type", "chuẩn") or name)
        capacity = parse_capacity_gb(_find(specs, "dung lượng", "capacity") or name)
        speed    = parse_speed_mhz(_find(specs, "tốc độ bus", "tốc độ", "speed", "bus") or name)
        modules  = _int(_find(specs, "số thanh", "kit", "x")) or 1

        results.append(Memory(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            Type=memtype,
            Capacity=capacity,
            Modules=modules,
            Speed=speed,
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} Memory")
    return results


# ── Motherboard ──────────────────────────────────────────────────────────────────

def scrape_motherboards() -> list[Motherboard]:
    print("\n[AnPhat Motherboard]")
    results = []
    for item in _get_all_products([1315, 1316]):
        name = item.get("productName", "").strip()
        if not name or "server" in name.lower():
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        socket_raw = _find(specs, "socket", "cpu socket")
        ff_raw     = _find(specs, "kích thước", "form factor", "chuẩn mainboard")
        mem_spec   = _find(specs, "khe cắm ram", "loại ram", "memory type")
        memtype    = normalize_memory_type(_ddr_type_or_name(mem_spec, name))
        slots_raw  = _find(specs, "khe cắm ram", "memory slots", "số khe")
        slots      = _int(slots_raw) or 4

        socket  = normalize_socket(_extract_socket(socket_raw) if socket_raw else "")
        ff      = normalize_form_factor(ff_raw or "")

        if not socket: socket = _mb_socket_from_name(name)
        socket = socket[:50]

        results.append(Motherboard(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            SocketCompatibility=socket or "Unknown",
            FormFactor=ff,
            MemoryCompatibility=memtype,
            MemorySlots=slots,
            MaxMemoryCapacity=128,
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} Motherboards")
    return results


# ── PSU ─────────────────────────────────────────────────────────────────────────

def scrape_power_supplies() -> list[PowerSupply]:
    print("\n[AnPhat PSU]")
    results = []
    for item in _get_all_products([1051]):
        name = item.get("productName", "").strip()
        if not name:
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        wattage    = parse_wattage(_find(specs, "công suất tối đa", "công suất", "wattage") or name)
        efficiency = normalize_efficiency(_find(specs, "chuẩn nguồn", "hiệu suất", "efficiency") or "")
        modular    = (_find(specs, "kiểu cáp nguồn", "modular", "cable") or "Non")[:20]

        results.append(PowerSupply(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            Wattage=wattage,
            Efficiency=efficiency,
            Modular=modular,
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} PSUs")
    return results


# ── Case ─────────────────────────────────────────────────────────────────────────

def scrape_cases() -> list[CaseEnclosure]:
    print("\n[AnPhat Case]")
    results = []
    for item in _get_all_products([1050]):
        name = item.get("productName", "").strip()
        if not name:
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        ff_raw  = _find(specs, "mainboard", "form factor", "hỗ trợ mainboard", "kích thước")
        max_vga = parse_length_mm(_find(specs, "gpu", "vga", "card màn hình", "chiều dài"))
        color   = (_find(specs, "màu sắc", "color") or "")[:30] or None
        ff      = normalize_form_factor(ff_raw or "ATX")

        results.append(CaseEnclosure(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            FormFactorSupport=ff,
            MaxVGALength=max_vga or 350,
            Color=color,
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} Cases")
    return results


# ── Storage ───────────────────────────────────────────────────────────────────────

def scrape_storage() -> list[Storage]:
    print("\n[AnPhat Storage]")
    results = []
    for cat_id, expected_type in [(1030, "SSD"), (1047, "HDD")]:
        for item in _get_all_products([cat_id]):
            name = item.get("productName", "").strip()
            if not name:
                continue
            price = int(item.get("price", 0))
            specs = _parse_summary(item.get("productSummary", ""))
            name_l = name.lower()

            if "nvme" in name_l or "m.2" in name_l or "nvm" in name_l:
                stor_type, interface = "NVMe", "M.2"
            elif expected_type == "SSD":
                stor_type, interface = "SSD", "SATA"
            else:
                stor_type, interface = "HDD", "SATA"

            capacity = parse_capacity_gb(
                _find(specs, "total capacity", "dung lượng", "capacity") or name
            )
            # speeds may be "Up to 4,000 MB/s" — strip commas before parsing
            def _speed(raw: str) -> int:
                val = re.sub(r",", "", raw)
                m = re.search(r"(\d+)", val)
                return int(m.group(1)) if m else 0
            read_speed  = _speed(_find(specs, "sequential read", "tốc độ đọc", "read"))
            write_speed = _speed(_find(specs, "sequential write", "tốc độ ghi", "write"))

            results.append(Storage(
                Name=name,
                Manufacturer=extract_manufacturer_from_name(name),
                Price=price,
                Type=stor_type,
                Capacity=capacity,
                Interface=interface,
                ReadSpeed=read_speed,
                WriteSpeed=write_speed,
                ImageUrl=_image(item),
                Stock=1,
            ))
    print(f"  → {len(results)} Storage")
    return results


# ── CPU Cooler ────────────────────────────────────────────────────────────────────

def scrape_cpu_coolers() -> list[CpuCooler]:
    print("\n[AnPhat Cooler]")
    results = []
    for item in _get_all_products([1390, 1392]):
        name = item.get("productName", "").strip()
        if not name:
            continue
        price = int(item.get("price", 0))
        specs = _parse_summary(item.get("productSummary", ""))

        socket_raw = _find(specs, "cpu socket", "socket hỗ trợ", "socket", "tương thích")
        max_tdp    = parse_tdp_watts(_find(specs, "tdp", "công suất tối đa", "max tdp"))
        height     = parse_length_mm(_find(specs, "chiều cao", "height", "dimension"))
        name_l     = name.lower()
        cooler_type = (
            "AIO-360" if "360" in name_l else
            "AIO-240" if ("aio" in name_l or "240" in name_l or "liquid" in name_l) else
            "Air"
        )

        results.append(CpuCooler(
            Name=name,
            Manufacturer=extract_manufacturer_from_name(name),
            Price=price,
            SocketCompatibility=(socket_raw or "Universal")[:200],
            MaxTDP=max_tdp or 150,
            Height=height or 160,
            Type=cooler_type,
            ImageUrl=_image(item),
            Stock=1,
        ))
    print(f"  → {len(results)} Coolers")
    return results
