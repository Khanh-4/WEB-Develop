"""Backfill: repair CPU rows with missing or estimated clock data.

Complements fix_cpu_clocks.py (which only targets clocks > 7 GHz). Two classes
of bad rows remain in the DB because the upsert never updates existing names:

  1. Broken rows — Boost/Base/Perf = 0 (scraped before the model lookup tables
     existed), or Base >= Boost (impossible), so they rank as 0 and the
     Compatibility Engine never picks them.
  2. Estimated base — names that carry only the boost ("UP TO 5.8GHZ") had their
     base guessed as 0.85 * boost (e.g. a wrong 4.93 for an i9-14900). The new
     _CPU_BASE_GHZ table gives the true base (2.0 / 3.2).

Recomputes BoostClock (_boost_clock_from_name), BaseClock (_base_clock_from_name)
and ApproximatePerformance (score_cpu) for affected rows only.

Run:
    cd scraper && source venv/bin/activate && python fix_cpu_data.py          # dry-run
    cd scraper && source venv/bin/activate && python fix_cpu_data.py --apply  # write
"""
import os
import sys

import psycopg2
from dotenv import load_dotenv

from scrapers.phongvu import _boost_clock_from_name, _base_clock_from_name, _cores_from_name
from scoring.performance import score_cpu

APPLY = "--apply" in sys.argv
MAX_PLAUSIBLE_GHZ = 7.0


def _is_estimated_base(base_old: float, boost_old: float) -> bool:
    """True when the stored base looks like the legacy 0.85 * boost estimate."""
    return boost_old > 0 and abs(base_old - round(boost_old * 0.85, 2)) < 0.02


def fix(cur):
    cur.execute(
        'SELECT "Id","Name","CoreCount","TDP","BaseClock","BoostClock",'
        '"ApproximatePerformance" FROM cpu ORDER BY "Id"'
    )
    rows = cur.fetchall()
    fixed = skipped = ok = 0

    for cpu_id, name, cores_old, tdp, base_old, boost_old, perf_old in rows:
        base_old = float(base_old or 0)
        boost_old = float(boost_old or 0)
        perf_old = float(perf_old or 0)
        cores_old = int(cores_old or 0)

        cores_new = cores_old or _cores_from_name(name)

        boost_new = _boost_clock_from_name(name)
        if boost_new <= 0:
            boost_new = boost_old if 0 < boost_old <= MAX_PLAUSIBLE_GHZ else 0
        if boost_new <= 0:
            if boost_old <= 0 or perf_old <= 0:
                print(f"  [SKIP] Id={cpu_id}: no boost from name — {name[:55]}")
                skipped += 1
            continue

        base_new = _base_clock_from_name(name, boost_new)
        perf_new = score_cpu(cores_new, base_new, boost_new, tdp)

        broken = (
            boost_old <= 0 or perf_old <= 0 or base_old <= 0
            or base_old >= boost_old
            or boost_old > MAX_PLAUSIBLE_GHZ or base_old > MAX_PLAUSIBLE_GHZ
            or cores_new != cores_old
            or (_is_estimated_base(base_old, boost_old) and base_new != base_old)
        )
        if not broken:
            ok += 1
            continue

        print(f"  Id={cpu_id}: {name[:55]}")
        print(f"     Cores {cores_old}->{cores_new}  Base {base_old}->{base_new}  Boost {boost_old}->{boost_new}  Perf {perf_old}->{perf_new}")
        fixed += 1
        if APPLY:
            cur.execute(
                'UPDATE cpu SET "CoreCount"=%s,"BaseClock"=%s,"BoostClock"=%s,"ApproximatePerformance"=%s WHERE "Id"=%s',
                (cores_new, base_new, boost_new, perf_new, cpu_id),
            )

    print(f"\nfixed={fixed}  already_ok={ok}  skipped={skipped}  total={len(rows)}")


if __name__ == "__main__":
    load_dotenv(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".env"))
    url = os.environ["DATABASE_URL"].replace("postgresql+psycopg2", "postgresql")
    conn = psycopg2.connect(url)
    cur = conn.cursor()

    print(f"=== CPU data fix ({'APPLY' if APPLY else 'DRY-RUN'}) ===\n")
    fix(cur)

    if APPLY:
        conn.commit()
        print("Committed.")
    else:
        print("Dry-run only. Re-run with --apply to write.")
    conn.close()
