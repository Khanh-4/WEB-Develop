"""One-time backfill: clean & infer CPU/Motherboard socket values in the DB.

Why: ~65% of motherboards had SocketCompatibility = 'Unknown' and several rows
held malformed tokens ('INTELLGA1700', '1700', 'AMDAM5', 'LGAAM5'). The
CompatibilityEngine treats 'Unknown' as a wildcard and can't match malformed
tokens to Cpu.Socket, so CPU<->MB compatibility filtering was mostly disabled.

This script reuses the same logic now in the scraper (normalize_socket +
chipset-from-name inference) to fix existing rows. Run once:

    cd scraper && source venv/bin/activate && python backfill_socket.py          # dry-run
    cd scraper && source venv/bin/activate && python backfill_socket.py --apply  # write
"""
import os
import sys
from collections import Counter

import psycopg2
from dotenv import load_dotenv

from processors.normalizer import normalize_socket
from scrapers.phongvu import _mb_socket_from_name, _socket_from_name

# Canonical sockets we trust to assert compatibility on.
VALID = {"LGA1700", "LGA1851", "LGA1200", "LGA1151", "AM5", "AM4", "AM3+"}


def resolve_mb(current: str, name: str) -> str:
    """Best canonical socket for a motherboard row, '' if undecidable."""
    cleaned = normalize_socket(current)
    if cleaned in VALID:
        return cleaned
    return _mb_socket_from_name(name or "")


def resolve_cpu(current: str, name: str) -> str:
    cleaned = normalize_socket(current)
    if cleaned in VALID:
        return cleaned
    return _socket_from_name(name or "")


def backfill(cur, table: str, col: str, resolver) -> Counter:
    stats = Counter()
    cur.execute(f'SELECT "Id", "Name", "{col}" FROM {table}')
    updates = []
    for _id, name, current in cur.fetchall():
        cur_norm = (current or "").strip()
        new = resolver(cur_norm, name)
        if not new:
            stats["still_unknown"] += 1
            continue
        if new == cur_norm:
            stats["already_ok"] += 1
            continue
        stats["fixed"] += 1
        updates.append((new, _id))
    if APPLY and updates:
        cur.executemany(f'UPDATE {table} SET "{col}" = %s WHERE "Id" = %s', updates)
    return stats


APPLY = "--apply" in sys.argv

if __name__ == "__main__":
    load_dotenv()
    url = os.environ["DATABASE_URL"].replace("postgresql+psycopg2", "postgresql")
    conn = psycopg2.connect(url)
    cur = conn.cursor()

    mode = "APPLY" if APPLY else "DRY-RUN"
    print(f"=== Socket backfill ({mode}) ===")
    for table, col, resolver in [
        ("motherboard", "SocketCompatibility", resolve_mb),
        ("cpu", "Socket", resolve_cpu),
    ]:
        s = backfill(cur, table, col, resolver)
        print(f"{table:12} fixed={s['fixed']:4}  already_ok={s['already_ok']:4}  still_unknown={s['still_unknown']:4}")

    if APPLY:
        conn.commit()
        print("Committed.")
        # Post-apply distribution
        cur.execute('SELECT "SocketCompatibility", count(*) FROM motherboard GROUP BY 1 ORDER BY 2 DESC')
        print("--- motherboard socket distribution after ---")
        for val, n in cur.fetchall():
            print(f"  {n:>4}  {val!r}")
    else:
        print("Dry-run only. Re-run with --apply to write.")
    conn.close()
