using TechSpecs.Data;
using TechSpecs.Services;
using TechSpecs.Tests.Helpers;
using TechSpecs.ViewModels.Builder;
using Xunit;

namespace TechSpecs.Tests;

/// <summary>
/// Tests for CompatibilityEngine Pass 2 rules (hard constraints).
/// Pass 1 (ILike search/budget) requires Postgres — leave SearchQuery/Brand null.
/// </summary>
public class CompatibilityEngineTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CompatibilityEngine _engine;

    public CompatibilityEngineTests()
    {
        _db = DbContextFactory.CreateSeeded($"compat_{Guid.NewGuid()}");
        _engine = new CompatibilityEngine(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── CPU ──────────────────────────────────────────────────────

    [Fact]
    public async Task Cpus_WithMatchingMbSocket_AreCompatible()
    {
        // MB id=10 is LGA1700, CPU id=1 is LGA1700
        var result = await _engine.FilterAsync(new BuildState { SelectedMotherboardId = 10 });

        var cpu = result.Cpus.First(c => c.Id == 1);
        Assert.True(cpu.IsCompatible);
    }

    [Fact]
    public async Task Cpus_WithMismatchedMbSocket_AreIncompatible()
    {
        // MB id=11 is AM5; CPU id=1 is LGA1700 → mismatch
        var result = await _engine.FilterAsync(new BuildState { SelectedMotherboardId = 11 });

        var cpu = result.Cpus.First(c => c.Id == 1);
        Assert.False(cpu.IsCompatible);
        Assert.Contains("Socket mismatch", cpu.IncompatibleReason);
    }

    [Fact]
    public async Task Cpus_WithUnknownMbSocket_AreCompatible()
    {
        // Unknown socket should not block any CPU
        var mb = _db.Motherboards.Find(10)!;
        mb.SocketCompatibility = "Unknown";
        await _db.SaveChangesAsync();

        var result = await _engine.FilterAsync(new BuildState { SelectedMotherboardId = 10 });

        Assert.All(result.Cpus, c => Assert.True(c.IsCompatible));
    }

    // ── Motherboard ──────────────────────────────────────────────

    [Fact]
    public async Task Motherboards_WithMatchingCpuSocket_AreCompatible()
    {
        // CPU id=1 is LGA1700; MB id=10 is LGA1700
        var result = await _engine.FilterAsync(new BuildState { SelectedCpuId = 1 });

        var mb = result.Motherboards.First(m => m.Id == 10);
        Assert.True(mb.IsCompatible);
    }

    [Fact]
    public async Task Motherboards_WithMismatchedCpuSocket_AreIncompatible()
    {
        // CPU id=2 is AM5; MB id=10 is LGA1700 → mismatch
        var result = await _engine.FilterAsync(new BuildState { SelectedCpuId = 2 });

        var mb = result.Motherboards.First(m => m.Id == 10);
        Assert.False(mb.IsCompatible);
        Assert.Contains("Socket mismatch", mb.IncompatibleReason);
    }

    [Fact]
    public async Task Motherboards_WithMismatchedRamType_AreIncompatible()
    {
        // RAM id=21 is DDR4; MB id=10 requires DDR5 → mismatch
        var result = await _engine.FilterAsync(new BuildState { SelectedMemoryId = 21 });

        var mb = result.Motherboards.First(m => m.Id == 10);
        Assert.False(mb.IsCompatible);
        Assert.Contains("DDR4", mb.IncompatibleReason!);
    }

    [Fact]
    public async Task Motherboards_WithUnsupportedCaseFormFactor_AreIncompatible()
    {
        // Case id=52 only supports ITX; MB id=10 is ATX → mismatch
        var result = await _engine.FilterAsync(new BuildState { SelectedCaseId = 52 });

        var mb = result.Motherboards.First(m => m.Id == 10);
        Assert.False(mb.IsCompatible);
        Assert.Contains("ATX", mb.IncompatibleReason!);
    }

    // ── GPU ──────────────────────────────────────────────────────

    [Fact]
    public async Task Gpus_ThatFitInCase_AreCompatible()
    {
        // Case id=50 MaxVGA=360mm; GPU id=30 is 310mm → fits
        var result = await _engine.FilterAsync(new BuildState { SelectedCaseId = 50 });

        var gpu = result.VideoCards.First(g => g.Id == 30);
        Assert.True(gpu.IsCompatible);
    }

    [Fact]
    public async Task Gpus_TooLongForCase_AreIncompatible()
    {
        // Case id=51 MaxVGA=315mm; GPU id=32 is 340mm → too long
        var result = await _engine.FilterAsync(new BuildState { SelectedCaseId = 51 });

        var gpu = result.VideoCards.First(g => g.Id == 32);
        Assert.False(gpu.IsCompatible);
        Assert.Contains("Too long", gpu.IncompatibleReason!);
    }

    [Fact]
    public async Task Gpus_WithUnderpoweredPsu_AreIncompatible()
    {
        // PSU id=41 is 550W; GPU id=32 TDP=450W + CPU default 65W = 515 * 1.2 = 618W needed
        var result = await _engine.FilterAsync(new BuildState { SelectedPowerSupplyId = 41 });

        var gpu = result.VideoCards.First(g => g.Id == 32);
        Assert.False(gpu.IsCompatible);
        Assert.Contains("PSU", gpu.IncompatibleReason!);
    }

    [Fact]
    public async Task Gpus_WithSufficientPsu_AreCompatible()
    {
        // PSU id=40 is 850W; GPU id=30 TDP=200W + CPU id=1 TDP=125W = 325 * 1.2 = 390W needed
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedPowerSupplyId = 40,
            SelectedCpuId = 1
        });

        var gpu = result.VideoCards.First(g => g.Id == 30);
        Assert.True(gpu.IsCompatible);
    }

    // ── PSU ──────────────────────────────────────────────────────

    [Fact]
    public async Task Psu_WattageRequirement_IsCalculatedCorrectly()
    {
        // CPU id=1 TDP=125W + GPU id=30 TDP=200W = 325W * 1.3 = 423W min
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedCpuId = 1,
            SelectedVideoCardId = 30
        });

        Assert.Equal(423, result.RecommendedPsuWattage);
    }

    [Fact]
    public async Task Psu_BelowMinWattage_IsIncompatible()
    {
        // CPU id=1 TDP=125 + GPU id=30 TDP=200 = 325 * 1.3 = 423W min
        // PSU id=41 is 550W → compatible; PSU id=42 is 1000W → compatible
        // Let's set GPU TDP very high to make PSU 850 fail
        var gpu = _db.VideoCards.Find(30)!;
        gpu.TDP = 600;
        await _db.SaveChangesAsync();

        // 125 + 600 = 725 * 1.3 = 942.5 → ceil = 943W
        // PSU id=40 (850W) < 943W → incompatible
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedCpuId = 1,
            SelectedVideoCardId = 30
        });

        var psu = result.PowerSupplies.First(p => p.Id == 40);
        Assert.False(psu.IsCompatible);
    }

    [Fact]
    public async Task Psu_AtOrAboveMinWattage_IsCompatible()
    {
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedCpuId = 1,    // TDP 125
            SelectedVideoCardId = 30  // TDP 200 → min = ceil(325*1.3) = 423W
        });

        var psu850 = result.PowerSupplies.First(p => p.Id == 40);
        var psu550 = result.PowerSupplies.First(p => p.Id == 41);
        Assert.True(psu850.IsCompatible);
        Assert.True(psu550.IsCompatible);
    }

    // ── Cooler ──────────────────────────────────────────────────

    [Fact]
    public async Task Cooler_WithMatchingSocket_IsCompatible()
    {
        // CPU id=1 is LGA1700; Cooler id=60 supports LGA1700
        var result = await _engine.FilterAsync(new BuildState { SelectedCpuId = 1 });

        var cooler = result.Coolers.First(c => c.Id == 60);
        Assert.True(cooler.IsCompatible);
    }

    [Fact]
    public async Task Cooler_WithMismatchedSocket_IsIncompatible()
    {
        // CPU id=2 is AM5; Cooler id=62 only supports LGA1700,LGA1200
        var result = await _engine.FilterAsync(new BuildState { SelectedCpuId = 2 });

        var cooler = result.Coolers.First(c => c.Id == 62);
        Assert.False(cooler.IsCompatible);
        Assert.Contains("AM5", cooler.IncompatibleReason!);
    }

    [Fact]
    public async Task Cooler_WithInsufficientTdp_IsIncompatible()
    {
        // CPU id=1 TDP=125W; Cooler id=62 MaxTDP=130 → compatible
        // Set CPU TDP higher to make it fail
        var cpu = _db.Cpus.Find(1)!;
        cpu.TDP = 200;
        await _db.SaveChangesAsync();

        var result = await _engine.FilterAsync(new BuildState { SelectedCpuId = 1 });

        var cooler = result.Coolers.First(c => c.Id == 62);
        Assert.False(cooler.IsCompatible);
        Assert.Contains("Cooling capacity", cooler.IncompatibleReason!);
    }

    // ── TDP totals ───────────────────────────────────────────────

    [Fact]
    public async Task TotalTdp_SumsCpuAndGpu()
    {
        // CPU id=1 TDP=125, GPU id=30 TDP=200
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedCpuId = 1,
            SelectedVideoCardId = 30
        });

        Assert.Equal(325, result.TotalTDP);
    }

    [Fact]
    public async Task RecommendedPsuWattage_Is130PercentOfTotalTdp()
    {
        var result = await _engine.FilterAsync(new BuildState
        {
            SelectedCpuId = 1,    // TDP 125
            SelectedVideoCardId = 30  // TDP 200
        });

        // ceil((125 + 200) * 1.3) = ceil(422.5) = 423
        Assert.Equal(423, result.RecommendedPsuWattage);
    }

    // ── Pass 3: Scoring ──────────────────────────────────────────

    [Fact]
    public async Task Results_ReturnAtMost30Items()
    {
        var result = await _engine.FilterAsync(new BuildState());

        Assert.True(result.Cpus.Count <= 30);
        Assert.True(result.Motherboards.Count <= 30);
        Assert.True(result.VideoCards.Count <= 30);
    }

    [Fact]
    public async Task IncompatibleItems_AppearAfterCompatibleOnes()
    {
        // With AM5 MB selected, LGA1700 CPUs should be incompatible and appear after AM5 ones
        var result = await _engine.FilterAsync(new BuildState { SelectedMotherboardId = 11 }); // AM5

        var compatible = result.Cpus.TakeWhile(c => c.IsCompatible).ToList();
        var incompatible = result.Cpus.SkipWhile(c => c.IsCompatible).ToList();

        // All compatible items appear before any incompatible
        Assert.All(incompatible, c => Assert.False(c.IsCompatible));
        if (compatible.Any() && incompatible.Any())
            Assert.True(result.Cpus.IndexOf(compatible.Last()) < result.Cpus.IndexOf(incompatible.First()));
    }
}
