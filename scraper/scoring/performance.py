"""
ApproximatePerformance heuristic scores for CPU and GPU.
These are relative scores (not benchmarks) used only for P/P ranking.
Higher = better performance per price in the Compatibility Engine.
"""


def score_cpu(core_count: int, base_clock: float, boost_clock: float, tdp: int) -> float:
    """
    Heuristic: weighted sum of cores * boost clock, penalized slightly by TDP.
    Tuned so modern mid-range CPUs land around 50-70, flagships around 90-100.
    """
    if core_count == 0 or boost_clock == 0:
        return 0.0

    effective_clock = boost_clock if boost_clock > 0 else base_clock
    raw = (core_count ** 0.75) * effective_clock * 10

    # Slight efficiency penalty — high TDP with low cores scores worse
    if tdp > 0:
        raw *= (1 - (tdp / 1000) * 0.05)

    return round(min(raw, 999.99), 2)


# GPU tier map: keyword → base score (VRAM and TDP will adjust)
_GPU_TIER: dict[str, float] = {
    # NVIDIA RTX 5000
    "5090": 999, "5080": 970, "5070 ti": 920, "5070": 860,
    "5060 ti": 780, "5060": 700,
    # NVIDIA RTX 4000
    "4090": 950, "4080 super": 880, "4080": 850, "4070 ti super": 820,
    "4070 ti": 790, "4070 super": 760, "4070": 720, "4060 ti": 650,
    "4060": 580,
    # NVIDIA RTX 3000
    "3090 ti": 870, "3090": 840, "3080 ti": 800,
    "3080": 770, "3070 ti": 720, "3070": 680, "3060 ti": 620,
    "3060": 560, "3050": 450,
    # AMD RX 9000
    "9070 xt": 870, "9070": 820, "9060 xt": 720,
    # AMD RX 7000
    "7900 xtx": 930, "7900 xt": 890, "7900 gre": 840, "7800 xt": 780,
    "7700 xt": 720, "7600 xt": 650, "7600": 590,
    # AMD RX 6000
    "6950 xt": 860, "6900 xt": 820, "6800 xt": 780, "6750 xt": 710,
    "6700 xt": 670, "6650 xt": 600, "6600 xt": 560, "6600": 510,
}


def score_gpu(name: str, vram: int, tdp: int) -> float:
    """
    Look up GPU tier score from name, then adjust by VRAM and efficiency.
    """
    name_lower = name.lower()
    base = 0.0
    for keyword, tier_score in sorted(_GPU_TIER.items(), key=lambda x: -len(x[0])):
        if keyword in name_lower:
            base = tier_score
            break

    if base == 0:
        # Fallback: rough estimate from VRAM
        base = min(vram * 50, 400)

    # Small VRAM bonus (each extra GB above 8 adds ~5 pts)
    vram_bonus = max(0, (vram - 8)) * 5

    # Efficiency: if TDP is available, reward cards that do more per watt
    efficiency_bonus = 0.0
    if tdp > 0:
        efficiency_bonus = (base / tdp) * 2

    return round(min(base + vram_bonus + efficiency_bonus, 999.99), 2)
