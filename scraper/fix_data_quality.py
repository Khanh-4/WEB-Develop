"""One-time data-quality fixes for Check lỗi V2 (Đợt 3).

Fixes three issues found in the production hardware tables:

  #10  Brand filter shows junk ("Liên", "RAM", "Vỏ", "Ổ", "Nguồn/") because the
       old extract_manufacturer_from_name() fell back to the product name's
       first word. Re-derive Manufacturer for every row with the rewritten
       allowlist-based function (aliases collapse, unmatched -> "Khác").

  #12  Full prebuilt desktop PCs ("PC GVN ...") were scraped into the `cpu`
       table. Remove them — prebuilts live in the dedicated `prebuilt_pc` table.

  #13  Laptops ("Laptop MSI ...") were scraped into the `video_card` table.
       Remove them — this site sells components, not laptops.

Run:
    cd scraper && source venv/bin/activate && python fix_data_quality.py          # dry-run
    cd scraper && source venv/bin/activate && python fix_data_quality.py --apply  # write
"""
import os
import sys
from collections import Counter

import psycopg2
from dotenv import load_dotenv

from processors.normalizer import extract_manufacturer_from_name

APPLY = "--apply" in sys.argv

TABLES = ["cpu", "motherboard", "memory", "video_card",
          "power_supply", "case_enclosure", "storage", "cpu_cooler"]

# Names that identify a row as a full system / laptop rather than a component.
# Per-table: a "Workstation" VGA is a legitimate GPU, so that keyword only
# flags prebuilt desktops in the `cpu` table — never the `video_card` table.
MISPLACED_LIKE = {
    "cpu": ["%máy bộ%", "%bộ máy%", "%pc gvn%", "%pc gaming%",
            "%gaming pc%", "%workstation%", "%laptop%", "%notebook%", "%macbook%"],
    "video_card": ["%laptop%", "%notebook%", "%macbook%"],
}


def backfill_manufacturer(cur) -> None:
    """#10 — recompute Manufacturer from Name for every hardware row."""
    print("=== #10 Manufacturer backfill ===")
    for t in TABLES:
        cur.execute(f'SELECT "Id", "Name", "Manufacturer" FROM {t}')
        rows = cur.fetchall()
        updates, before, after = [], Counter(), Counter()
        for _id, name, current in rows:
            new = extract_manufacturer_from_name(name or "")
            before[(current or "").strip()] += 1
            after[new] += 1
            if new != (current or "").strip():
                updates.append((new, _id))
        if APPLY and updates:
            cur.executemany(f'UPDATE {t} SET "Manufacturer" = %s WHERE "Id" = %s', updates)
        print(f"  {t:14} rows={len(rows):4} changed={len(updates):4} "
              f"distinct {len(before):2}->{len(after):2}  "
              f"khac={after.get('Khác', 0)}")


def delete_misplaced(cur) -> None:
    """#12/#13 — remove prebuilt-PC rows from cpu and laptop rows from video_card."""
    print("\n=== #12 prebuilt PCs in `cpu` / #13 laptops in `video_card` ===")
    for table in ("cpu", "video_card"):
        patterns = MISPLACED_LIKE[table]
        where = " OR ".join('"Name" ILIKE %s' for _ in patterns)
        cur.execute(f'SELECT "Id", "Name" FROM {table} WHERE {where}', patterns)
        victims = cur.fetchall()
        print(f"\n  -- {table}: {len(victims)} misplaced row(s) --")
        for _id, name in victims:
            print(f"     [{_id}] {name[:80]}")
        if APPLY and victims:
            ids = [v[0] for v in victims]
            cur.execute(f'DELETE FROM {table} WHERE "Id" = ANY(%s)', (ids,))
            print(f"     -> deleted {len(ids)} row(s)")


if __name__ == "__main__":
    load_dotenv()
    url = os.environ["DATABASE_URL"].replace("postgresql+psycopg2", "postgresql")
    conn = psycopg2.connect(url)
    cur = conn.cursor()

    print(f"=== Data-quality fix ({'APPLY' if APPLY else 'DRY-RUN'}) ===\n")
    backfill_manufacturer(cur)
    delete_misplaced(cur)

    if APPLY:
        conn.commit()
        print("\nCommitted.")
    else:
        print("\nDry-run only. Re-run with --apply to write.")
    conn.close()
