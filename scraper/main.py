"""
TechSpecs Scraper
Usage:
    python main.py                         # scrape all categories from all sources
    python main.py cpu gpu ram             # specific categories, all sources
    python main.py cpu --source phongvu    # specific categories + specific source
    python main.py --source ttgshop        # all categories, one source

Valid sources: phongvu, ttgshop, gearvn, anphat
Valid categories: cpu gpu ram motherboard psu case storage cooler
"""

import sys
import os
from datetime import datetime, timezone
from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from sqlalchemy.orm import Session

load_dotenv()
DATABASE_URL = os.getenv("DATABASE_URL")
if not DATABASE_URL:
    raise RuntimeError("DATABASE_URL not set in .env")

engine = create_engine(DATABASE_URL, echo=False)

import scrapers.phongvu  as pv
import scrapers.ttgshop  as ttg
import scrapers.gearvn   as gvn
import scrapers.anphat   as ap
from models.hardware import Cpu, VideoCard, Memory, Motherboard, PowerSupply, CaseEnclosure, Storage, CpuCooler

ALL_CATS    = ["cpu", "gpu", "ram", "motherboard", "psu", "case", "storage", "cooler"]
ALL_SOURCES = ["phongvu", "ttgshop", "gearvn", "anphat"]

# Map DB table name → category slug used in price_history
_TABLE_CAT = {
    "cpu": "cpu", "video_card": "gpu", "memory": "ram",
    "motherboard": "motherboard", "power_supply": "psu",
    "case_enclosure": "case", "storage": "storage", "cpu_cooler": "cooler",
}


# Spec fields that can be filled/updated from scraper per table.
# Excludes: Id, Name, Manufacturer, Price, Stock, StockStatusOverride (manually managed).
# Strategy: only write if the DB value is currently empty/zero ("fill-in-the-gaps").
_SPEC_FIELDS: dict[str, list[str]] = {
    "motherboard":    ["Chipset", "SocketCompatibility", "FormFactor", "MemoryCompatibility",
                       "MemorySlots", "MaxMemoryCapacity", "ImageUrl"],
    "cpu":            ["Socket", "CoreCount", "ThreadCount", "BaseClock", "BoostClock",
                       "TDP", "ApproximatePerformance", "ImageUrl"],
    "video_card":     ["VRAM", "Length", "TDP", "ApproximatePerformance", "ImageUrl"],
    "memory":         ["Type", "Capacity", "Modules", "Speed", "Profile", "ImageUrl"],
    "storage":        ["Type", "Capacity", "Interface", "ReadSpeed", "WriteSpeed", "ImageUrl"],
    "power_supply":   ["Wattage", "Efficiency", "Modular", "PsuFormFactor", "ImageUrl"],
    "case_enclosure": ["FormFactorSupport", "MaxVGALength", "Color", "CaseType",
                       "RadiatorSupport", "ImageUrl"],
    "cpu_cooler":     ["SocketCompatibility", "MaxTDP", "Height", "Type", "ImageUrl"],
}


def _is_empty(val) -> bool:
    """True when a DB field is considered unfilled (empty string or zero)."""
    if val is None:
        return True
    if isinstance(val, str):
        return val.strip() == ""
    return val == 0


def upsert(session: Session, items: list, table: str):
    if not items:
        print("  (no items to insert)")
        return

    category   = _TABLE_CAT.get(table, table)
    now        = datetime.now(timezone.utc)
    spec_fields = _SPEC_FIELDS.get(table, [])

    # Fetch existing rows: name → {price, <spec fields...>}
    extra_cols = (", " + ", ".join(f'"{f}"' for f in spec_fields)) if spec_fields else ""
    rows = session.execute(text(f'SELECT "Name", "Price"{extra_cols} FROM "{table}"'))
    existing: dict[str, dict] = {}
    for row in rows:
        existing[row[0]] = {
            "price": row[1],
            **{spec_fields[i]: row[i + 2] for i in range(len(spec_fields))},
        }

    new_items, price_records, price_updates, spec_updates = [], [], [], []

    for item in items:
        if item.Name not in existing:
            # Brand new product — insert
            new_items.append(item)
            if item.Price > 0:
                price_records.append({"category": category, "product_name": item.Name,
                                      "price": float(item.Price), "recorded_at": now})
        else:
            db_row   = existing[item.Name]
            old_price = db_row["price"]

            # Price changed → update
            if item.Price > 0 and item.Price != old_price:
                price_updates.append({"name": item.Name, "price": float(item.Price)})
                price_records.append({"category": category, "product_name": item.Name,
                                      "price": float(item.Price), "recorded_at": now})

            # Spec fill-in: only update fields that are currently empty in DB
            upd: dict = {}
            for field in spec_fields:
                db_val  = db_row.get(field)
                new_val = getattr(item, field, None)
                if _is_empty(db_val) and not _is_empty(new_val):
                    upd[field] = new_val
            if upd:
                upd["name"] = item.Name
                spec_updates.append(upd)

    if new_items:
        session.add_all(new_items)

    for upd in price_updates:
        session.execute(text(f'UPDATE "{table}" SET "Price" = :price WHERE "Name" = :name'), upd)

    for upd in spec_updates:
        cols = [k for k in upd if k != "name"]
        set_clause = ", ".join(f'"{c}" = :{c}' for c in cols)
        session.execute(text(f'UPDATE "{table}" SET {set_clause} WHERE "Name" = :name'), upd)

    if price_records:
        session.execute(
            text('INSERT INTO price_history ("Category", "ProductName", "Price", "RecordedAt") '
                 'VALUES (:category, :product_name, :price, :recorded_at)'),
            price_records,
        )

    session.commit()
    print(f"  ✓ {len(new_items)} inserted, {len(price_updates)} price updated, "
          f"{len(spec_updates)} spec filled, "
          f"{len(items) - len(new_items)} existing")


def run(cats: list[str], sources: list[str]):
    with Session(engine) as s:

        # ── PhongVu ─────────────────────────────────────────────────────────
        if "phongvu" in sources:
            print("\n=== PhongVu ===")
            if "cpu"         in cats: upsert(s, pv.scrape_cpus(),           "cpu")
            if "gpu"         in cats: upsert(s, pv.scrape_video_cards(),    "video_card")
            if "ram"         in cats: upsert(s, pv.scrape_memory(),         "memory")
            if "motherboard" in cats: upsert(s, pv.scrape_motherboards(),   "motherboard")
            if "psu"         in cats: upsert(s, pv.scrape_power_supplies(), "power_supply")
            if "case"        in cats: upsert(s, pv.scrape_cases(),          "case_enclosure")
            if "storage"     in cats: upsert(s, pv.scrape_storage(),        "storage")
            if "cooler"      in cats: upsert(s, pv.scrape_cpu_coolers(),    "cpu_cooler")

        # ── TTGShop ─────────────────────────────────────────────────────────
        if "ttgshop" in sources:
            print("\n=== TTGShop ===")
            if "cpu"         in cats: upsert(s, ttg.scrape_cpus(),           "cpu")
            if "gpu"         in cats: upsert(s, ttg.scrape_video_cards(),    "video_card")
            if "ram"         in cats: upsert(s, ttg.scrape_memory(),         "memory")
            if "motherboard" in cats: upsert(s, ttg.scrape_motherboards(),   "motherboard")
            if "psu"         in cats: upsert(s, ttg.scrape_power_supplies(), "power_supply")
            if "case"        in cats: upsert(s, ttg.scrape_cases(),          "case_enclosure")
            if "storage"     in cats: upsert(s, ttg.scrape_storage(),        "storage")
            if "cooler"      in cats: upsert(s, ttg.scrape_cpu_coolers(),    "cpu_cooler")

        # ── GearVN ──────────────────────────────────────────────────────────
        if "gearvn" in sources:
            print("\n=== GearVN ===")
            print("Fetching product sitemaps...")
            all_urls = gvn._get_all_product_urls()
            if "cpu"         in cats: upsert(s, gvn.scrape_cpus(all_urls),           "cpu")
            if "gpu"         in cats: upsert(s, gvn.scrape_video_cards(all_urls),    "video_card")
            if "ram"         in cats: upsert(s, gvn.scrape_memory(all_urls),         "memory")
            if "motherboard" in cats: upsert(s, gvn.scrape_motherboards(all_urls),   "motherboard")
            if "psu"         in cats: upsert(s, gvn.scrape_power_supplies(all_urls), "power_supply")
            if "case"        in cats: upsert(s, gvn.scrape_cases(all_urls),          "case_enclosure")
            if "storage"     in cats: upsert(s, gvn.scrape_storage(all_urls),        "storage")
            if "cooler"      in cats: upsert(s, gvn.scrape_cpu_coolers(all_urls),    "cpu_cooler")

        # ── An Phát ─────────────────────────────────────────────────────────
        if "anphat" in sources:
            print("\n=== An Phát ===")
            if "cpu"         in cats: upsert(s, ap.scrape_cpus(),           "cpu")
            if "gpu"         in cats: upsert(s, ap.scrape_video_cards(),    "video_card")
            if "ram"         in cats: upsert(s, ap.scrape_memory(),         "memory")
            if "motherboard" in cats: upsert(s, ap.scrape_motherboards(),   "motherboard")
            if "psu"         in cats: upsert(s, ap.scrape_power_supplies(), "power_supply")
            if "case"        in cats: upsert(s, ap.scrape_cases(),          "case_enclosure")
            if "storage"     in cats: upsert(s, ap.scrape_storage(),        "storage")
            if "cooler"      in cats: upsert(s, ap.scrape_cpu_coolers(),    "cpu_cooler")

    print("\nDone.")


if __name__ == "__main__":
    args = sys.argv[1:]

    # Parse --source flag
    sources = ALL_SOURCES
    if "--source" in args:
        idx = args.index("--source")
        source_val = args[idx + 1] if idx + 1 < len(args) else ""
        bad_src = [s for s in source_val.split(",") if s not in ALL_SOURCES]
        if bad_src:
            print(f"Unknown source(s): {bad_src}. Valid: {ALL_SOURCES}")
            sys.exit(1)
        sources = source_val.split(",")
        args = [a for i, a in enumerate(args) if i != idx and i != idx + 1]

    # Remaining args are category filters
    cats = [c.lower() for c in args] if args else ALL_CATS
    bad_cats = [c for c in cats if c not in ALL_CATS]
    if bad_cats:
        print(f"Unknown category(s): {bad_cats}. Valid: {ALL_CATS}")
        sys.exit(1)

    run(cats, sources)
