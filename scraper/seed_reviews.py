"""Seed realistic Vietnamese product reviews (Check lỗi V2 #4 — review realism).

The live DB had only 3 reviews, so every product showed no rating and the site
looked empty/unrealistic. This seeds varied, believable Vietnamese reviews for
every priced product across all 8 hardware tables.

Design / reversibility:
  * Reviews need a real AspNetUsers row (FK, ON DELETE CASCADE). We create a
    dedicated pool of reviewer accounts under the marker domain
    "@seedreviewer.local" — they have no password (display-only) and are 100%
    identifiable. `--undo` deletes those users, and CASCADE removes every seeded
    review with them. No real user data is touched.
  * Idempotent: a product that already has a review from a seed user is skipped,
    so re-running never double-seeds.

Run:
    cd scraper && source venv/bin/activate && python seed_reviews.py          # dry-run
    cd scraper && source venv/bin/activate && python seed_reviews.py --apply  # write
    cd scraper && source venv/bin/activate && python seed_reviews.py --undo   # remove all seed data
"""
import os
import random
import sys
import uuid
from datetime import datetime, timedelta, timezone

import psycopg2
from psycopg2.extras import execute_values
from dotenv import load_dotenv

APPLY = "--apply" in sys.argv
UNDO = "--undo" in sys.argv
SEED_DOMAIN = "@seedreviewer.local"
TARGET_USERS = 80

# (table, category-label used by the web). Category must match ProductReview.Category.
TABLES = [
    ("cpu", "cpu"), ("motherboard", "motherboard"), ("memory", "memory"),
    ("video_card", "gpu"), ("power_supply", "psu"), ("case_enclosure", "case"),
    ("storage", "storage"), ("cpu_cooler", "cooler"),
]

random.seed(42)  # reproducible seeding

# ── Vietnamese reviewer names ────────────────────────────────────────────────
_HO = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Vũ", "Võ", "Phan",
       "Trương", "Bùi", "Đặng", "Đỗ", "Ngô", "Dương", "Lý", "Đinh", "Mai"]
_DEM = ["Văn", "Hữu", "Đức", "Quốc", "Minh", "Thành", "Hoàng", "Anh", "Thị",
        "Ngọc", "Thu", "Gia", "Bảo", "Tuấn", "Hải", "Khánh", "Phương"]
_TEN = ["An", "Bình", "Cường", "Dũng", "Hùng", "Khoa", "Long", "Nam", "Phúc",
        "Quân", "Sơn", "Tài", "Thắng", "Trung", "Việt", "Hương", "Lan", "Linh",
        "Mai", "Nhung", "Trang", "Vy", "Yến", "Đạt", "Huy", "Kiên", "Lộc"]

# ── Comment pools by rating tier + per-category flavour ───────────────────────
_GENERIC = {
    5: ["Sản phẩm tuyệt vời, đóng gói cẩn thận, giao hàng nhanh.",
        "Rất hài lòng, đúng mô tả, hàng chính hãng.",
        "Chất lượng quá ổn so với giá tiền, sẽ ủng hộ shop tiếp.",
        "Hàng đẹp, nguyên seal, tư vấn nhiệt tình. 5 sao!",
        "Mua về lắp dùng ngon lành, không một lỗi nhỏ."],
    4: ["Sản phẩm tốt, giá hợp lý. Trừ nửa sao vì giao hơi chậm.",
        "Dùng ổn định, hài lòng. Đóng gói có thể chắc chắn hơn.",
        "Hàng đúng như hình, chất lượng khá. Đáng mua.",
        "Ổn trong tầm giá, shop hỗ trợ nhanh."],
    3: ["Tạm ổn so với tầm giá, không có gì quá nổi bật.",
        "Đúng mô tả nhưng đóng gói hơi sơ sài.",
        "Dùng được, kỳ vọng hơn một chút ở mức giá này.",
        "Bình thường, không quá xuất sắc."],
    2: ["Hiệu năng không như kỳ vọng, hơi thất vọng.",
        "Giao hàng chậm, sản phẩm có vết xước nhỏ.",
        "Chất lượng tạm, giá hơi cao so với giá trị."],
    1: ["Không hài lòng, sản phẩm không như quảng cáo.",
        "Giao sai, hỗ trợ chậm. Cân nhắc khi mua."],
}
_FLAVOUR = {
    "cpu": ["Chạy mát, ép xung nhẹ vẫn ổn định.", "Đa nhiệm mượt, nhiệt độ kiểm soát tốt."],
    "gpu": ["Chơi game max setting rất mượt.", "Quạt hơi ồn khi full tải nhưng nhiệt ổn."],
    "motherboard": ["Đầy đủ cổng, BIOS dễ dùng.", "Bo mạch chắc chắn, hỗ trợ tốt RAM bus cao."],
    "memory": ["Bật XMP/EXPO chạy ổn định.", "Tản đẹp, đèn RGB hợp gu."],
    "psu": ["Dây dợ đầy đủ, chạy êm và mát.", "Nguồn ổn định, không hú khi tải nặng."],
    "case": ["Lắp ráp dễ, thoáng khí, đi dây gọn.", "Thiết kế đẹp, gia công chắc chắn."],
    "storage": ["Tốc độ đọc ghi nhanh đúng quảng cáo.", "Cài win chép dữ liệu nhanh gọn."],
    "cooler": ["Tản nhiệt mát, lắp đặt dễ.", "Hạ nhiệt CPU thấy rõ, quạt êm."],
}


def vietnamese_names(n):
    names, seen = [], set()
    while len(names) < n:
        nm = f"{random.choice(_HO)} {random.choice(_DEM)} {random.choice(_TEN)}"
        if nm not in seen:
            seen.add(nm)
            names.append(nm)
    return names


def make_comment(rating, category):
    base = random.choice(_GENERIC[rating])
    if rating >= 4 and random.random() < 0.6:
        return f"{base} {random.choice(_FLAVOUR[category])}"
    return base


def weighted_rating(product_mean):
    """Sample a 1-5 rating clustered around a per-product mean (realistic spread)."""
    r = round(random.gauss(product_mean, 0.7))
    return max(1, min(5, r))


def ensure_seed_users(cur):
    """Create up to TARGET_USERS reviewer accounts; return list of (id, name)."""
    cur.execute('SELECT "Id", "FullName" FROM "AspNetUsers" WHERE "Email" LIKE %s',
                (f"%{SEED_DOMAIN}",))
    existing = cur.fetchall()
    if len(existing) >= TARGET_USERS:
        return existing
    names = vietnamese_names(TARGET_USERS)
    rows, out = [], list(existing)
    for i in range(len(existing), TARGET_USERS):
        uid = str(uuid.uuid4())
        name = names[i]
        email = f"reviewer.{i+1}{SEED_DOMAIN}"
        created = datetime.now(timezone.utc) - timedelta(days=random.randint(60, 600))
        rows.append((uid, name, created, email, email.upper(), email, email.upper(),
                     True, False, False, False, 0, 0, 0.0))
        out.append((uid, name))
    if APPLY and rows:
        execute_values(cur, '''INSERT INTO "AspNetUsers"
            ("Id","FullName","CreatedAt","UserName","NormalizedUserName","Email",
             "NormalizedEmail","EmailConfirmed","PhoneNumberConfirmed",
             "TwoFactorEnabled","LockoutEnabled","AccessFailedCount",
             "LoyaltyPoints","TotalSpend") VALUES %s''', rows)
    print(f"  seed users: {len(existing)} existing, {len(rows)} new -> {len(out)} total")
    return out


def seed_reviews(cur):
    users = ensure_seed_users(cur)
    if not users:
        print("  (dry-run) would create seed users first")
        users = [("dryrun", "Người Dùng")] * TARGET_USERS

    # products already carrying a seed review -> skip (idempotent)
    cur.execute('''SELECT DISTINCT r."Category", r."ComponentId" FROM product_reviews r
        JOIN "AspNetUsers" u ON u."Id" = r."UserId" WHERE u."Email" LIKE %s''',
                (f"%{SEED_DOMAIN}",))
    already = set(cur.fetchall())

    all_rows, per_cat = [], {}
    for table, category in TABLES:
        cur.execute(f'SELECT "Id" FROM {table} WHERE "Price" > 0')
        ids = [r[0] for r in cur.fetchall()]
        seeded = 0
        for cid in ids:
            if (category, cid) in already:
                continue
            product_mean = random.uniform(3.7, 4.8)   # per-product quality bias
            n = random.randint(2, 8)
            chosen = random.sample(users, min(n, len(users)))
            for uid, uname in chosen:
                rating = weighted_rating(product_mean)
                comment = make_comment(rating, category) if random.random() < 0.82 else None
                created = datetime.now(timezone.utc) - timedelta(
                    days=random.randint(1, 240), hours=random.randint(0, 23))
                all_rows.append((uid, category, cid, rating, comment, None, uname, created))
            seeded += 1
        per_cat[category] = (len(ids), seeded)

    if APPLY and all_rows:
        execute_values(cur, '''INSERT INTO product_reviews
            ("UserId","Category","ComponentId","Rating","Comment","ImageUrl",
             "UserDisplayName","CreatedAt") VALUES %s''', all_rows, page_size=1000)

    print("\n  per-category (priced products / products seeded):")
    for cat, (total, seeded) in per_cat.items():
        print(f"    {cat:12} {total:5} / seeded {seeded:5}")
    print(f"\n  total reviews to insert: {len(all_rows)}")


def undo(cur):
    cur.execute('SELECT count(*) FROM "AspNetUsers" WHERE "Email" LIKE %s', (f"%{SEED_DOMAIN}",))
    nu = cur.fetchone()[0]
    cur.execute('''SELECT count(*) FROM product_reviews r JOIN "AspNetUsers" u
        ON u."Id"=r."UserId" WHERE u."Email" LIKE %s''', (f"%{SEED_DOMAIN}",))
    nr = cur.fetchone()[0]
    print(f"  would delete {nu} seed users and {nr} seed reviews (CASCADE)")
    if APPLY:
        cur.execute('DELETE FROM "AspNetUsers" WHERE "Email" LIKE %s', (f"%{SEED_DOMAIN}",))
        print("  deleted.")


if __name__ == "__main__":
    load_dotenv()
    url = os.environ["DATABASE_URL"].replace("postgresql+psycopg2", "postgresql")
    conn = psycopg2.connect(url)
    cur = conn.cursor()

    mode = "APPLY" if APPLY else "DRY-RUN"
    if UNDO:
        print(f"=== Seed reviews UNDO ({mode}) ===")
        undo(cur)
    else:
        print(f"=== Seed reviews ({mode}) ===")
        seed_reviews(cur)

    if APPLY:
        conn.commit()
        print("\nCommitted.")
    else:
        print("\nDry-run only. Re-run with --apply to write (or --undo to remove).")
    conn.close()
