"""One-time fix for corrupted CPU clock data (Check lỗi V2 follow-up).

A handful of CPU rows had an implausible BoostClock (e.g. 90 GHz for an
i7-12700F whose true boost is 4.9 GHz). Root cause: parse_clock_ghz() mis-read
the source spec field and returned a junk value that beat the reliable
name-based fallback. BaseClock (derived as 0.85 * boost) and
ApproximatePerformance (score_cpu, pinned near its 999.99 cap) were corrupted
too, which polluted the Compatibility Engine's value ranking.

This recomputes BoostClock from the product name (_boost_clock_from_name),
BaseClock from the name's lowest GHz value (or 0.85 * boost as a fallback), and
ApproximatePerformance via score_cpu — for every cpu row with an impossible
clock (> 7 GHz). The parse_clock_ghz() hardening in normalizer.py prevents new
rows from getting corrupted on future scrapes.

Run:
    cd scraper && source venv/bin/activate && python fix_cpu_clocks.py          # dry-run
    cd scraper && source venv/bin/activate && python fix_cpu_clocks.py --apply  # write
"""
import os
import re
import sys

import psycopg2
from dotenv import load_dotenv

from scrapers.phongvu import _boost_clock_from_name
from scoring.performance import score_cpu

APPLY = "--apply" in sys.argv

# CPU clocks above this are physically impossible — flag the row for repair.
MAX_PLAUSIBLE_GHZ = 7.0


def _base_clock_from_name(name: str, boost: float) -> float:
    """Pick the base clock from the name's GHz values (lowest value below the
    boost), or estimate it as 85% of boost when the name has only one value."""
    vals = [float(v) for v in re.findall(r"([\d.]+)\s*ghz", name.lower())]
    below = [v for v in vals if 0.5 < v < boost]
    if below:
        return round(min(below), 2)
    return round(boost * 0.85, 2)


def fix_cpu_clocks(cur):
    cur.execute(
        'SELECT "Id", "Name", "CoreCount", "TDP", "BaseClock", "BoostClock", '
        '"ApproximatePerformance" FROM cpu '
        'WHERE "BoostClock" > %s OR "BaseClock" > %s ORDER BY "Id"',
        (MAX_PLAUSIBLE_GHZ, MAX_PLAUSIBLE_GHZ),
    )
    rows = cur.fetchall()
    print(f"Found {len(rows)} CPU row(s) with an impossible clock (> {MAX_PLAUSIBLE_GHZ} GHz)\n")

    for cpu_id, name, cores, tdp, base_old, boost_old, perf_old in rows:
        boost_new = _boost_clock_from_name(name)
        if boost_new <= 0:
            print(f"  [SKIP] Id={cpu_id}: could not derive boost from name — {name[:55]}")
            continue
        base_new = _base_clock_from_name(name, boost_new)
        perf_new = score_cpu(cores, base_new, boost_new, tdp)

        print(f"  Id={cpu_id}: {name[:55]}")
        print(f"     Base  {base_old} -> {base_new}")
        print(f"     Boost {boost_old} -> {boost_new}")
        print(f"     Perf  {perf_old} -> {perf_new}")

        if APPLY:
            cur.execute(
                'UPDATE cpu SET "BaseClock"=%s, "BoostClock"=%s, "ApproximatePerformance"=%s '
                'WHERE "Id"=%s',
                (base_new, boost_new, perf_new, cpu_id),
            )


if __name__ == "__main__":
    load_dotenv()
    url = os.environ["DATABASE_URL"].replace("postgresql+psycopg2", "postgresql")
    conn = psycopg2.connect(url)
    cur = conn.cursor()

    print(f"=== CPU clock fix ({'APPLY' if APPLY else 'DRY-RUN'}) ===\n")
    fix_cpu_clocks(cur)

    if APPLY:
        conn.commit()
        print("\nCommitted.")
    else:
        print("\nDry-run only. Re-run with --apply to write.")
    conn.close()
