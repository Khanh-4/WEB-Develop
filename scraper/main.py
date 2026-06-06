"""
TechSpecs Scraper
Usage:
    python main.py                         # scrape all categories from all sources
    python main.py cpu gpu ram             # specific categories, all sources
    python main.py cpu --source phongvu    # specific categories + specific source
    python main.py --source ttgshop        # all categories, one source

Valid sources: phongvu, ttgshop, gearvn
Valid categories: cpu gpu ram motherboard psu case storage cooler
"""

import sys
import os
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
from models.hardware import Cpu, VideoCard, Memory, Motherboard, PowerSupply, CaseEnclosure, Storage, CpuCooler

ALL_CATS    = ["cpu", "gpu", "ram", "motherboard", "psu", "case", "storage", "cooler"]
ALL_SOURCES = ["phongvu", "ttgshop", "gearvn"]


def upsert(session: Session, items: list, table: str):
    if not items:
        print("  (no items to insert)")
        return
    existing = {r[0] for r in session.execute(text(f'SELECT "Name" FROM "{table}"'))}
    new = [x for x in items if x.Name not in existing]
    if new:
        session.add_all(new)
        session.commit()
    print(f"  ✓ {len(new)} inserted, {len(items)-len(new)} skipped (duplicate)")


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
