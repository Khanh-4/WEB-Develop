using TechSpecs.ViewModels.Builder;

namespace TechSpecs.ViewModels;

// Legacy 2-build compare (kept for backward compat)
public class CompareRequest
{
    public BuildState BuildA { get; set; } = new();
    public BuildState BuildB { get; set; } = new();
}

public class BuildComparisonResult
{
    public BuildSnapshot A { get; set; } = new();
    public BuildSnapshot B { get; set; } = new();
}

// Multi-option compare (2–4 builds)
public class LabeledBuild
{
    public string Label { get; set; } = string.Empty;
    public BuildState State { get; set; } = new();
    public int? PrebuiltPcId { get; set; }  // when set, snapshot is loaded from prebuilt_pcs table
}

public class MultiCompareRequest
{
    public List<LabeledBuild> Builds { get; set; } = new();
}

public class LabeledSnapshot
{
    public string Label { get; set; } = string.Empty;
    public BuildSnapshot Data { get; set; } = new();
}

public class MultiCompareResult
{
    public List<LabeledSnapshot> Options { get; set; } = new();
}

// Cached compare session (stored in IMemoryCache, expires in 1h)
public class CompareSession
{
    public List<LabeledSnapshot> Options { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class BuildSnapshot
{
    public decimal TotalPrice { get; set; }
    public int TotalTDP { get; set; }
    public RadarScores Radar { get; set; } = new();
    public BuildSpecsDetail Specs { get; set; } = new();
    public BenchmarkData? Benchmark { get; set; }
    public bool BenchmarkIsReal { get; set; }
}

public class RadarScores
{
    public int Gaming { get; set; }         // 0–100
    public int Multitasking { get; set; }   // 0–100
    public int Storage { get; set; }        // 0–100
    public int Thermal { get; set; }        // 0–100
    public int Upgrade { get; set; }        // 0–100
}

public class BuildSpecsDetail
{
    // Group 2: Core Specs
    public ComponentSnap? Cpu { get; set; }
    public ComponentSnap? Gpu { get; set; }
    public ComponentSnap? Memory { get; set; }
    public ComponentSnap? Storage { get; set; }
    // Group 3: Future-proofing
    public ComponentSnap? Psu { get; set; }
    public ComponentSnap? Motherboard { get; set; }
    public ComponentSnap? Cooler { get; set; }
    // Group 4: Form Factor
    public ComponentSnap? Case { get; set; }
    // Derived
    public int PsuHeadroomW { get; set; }
    public int PsuHeadroomPct { get; set; }
    public int RamFreeSlots { get; set; }
    public string CoolerType { get; set; } = "—";
    public string CaseFormFactor { get; set; } = "—";
}

public class ComponentSnap
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    // Key stat values for diff display
    public Dictionary<string, string> KeyStats { get; set; } = new();
}

public class BenchmarkData
{
    // CPU
    public int? CinebenchMulti { get; set; }
    public int? CinebenchSingle { get; set; }
    // GPU
    public int? FpsCs2_1080p { get; set; }
    public int? FpsCs2_1440p { get; set; }
    public int? FpsCyberpunk_1080p { get; set; }
    public int? FpsCyberpunk_1440p { get; set; }
    public int? FpsValorant_1080p { get; set; }
    public int? FpsValorant_1440p { get; set; }
    // Estimated gaming score (from ApproxPerf tier if no real data)
    public int? EstimatedGamingScore { get; set; }
}
