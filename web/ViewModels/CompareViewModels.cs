using TechSpecs.ViewModels.Builder;

namespace TechSpecs.ViewModels;

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
