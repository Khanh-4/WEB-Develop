using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Services;

public class PrebuiltPcSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<PrebuiltPcSeeder> _logger;

    public PrebuiltPcSeeder(AppDbContext db, ILogger<PrebuiltPcSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _db.PrebuiltPcs.AnyAsync())
            return;

        _logger.LogInformation("Seeding prebuilt PCs from existing component data...");

        try
        {
            var profiles = new[]
            {
                ("PC Văn phòng",         "Office",  4_000_000m,  12_000_000m),
                ("PC Gaming tầm trung",  "Gaming",  10_000_000m, 25_000_000m),
                ("PC Gaming cao cấp",    "Gaming",  20_000_000m, 45_000_000m),
                ("PC Đồ hoạ / Thiết kế","Creator", 15_000_000m, 35_000_000m),
                ("PC Workstation",       "Creator", 25_000_000m, 60_000_000m),
            };

            foreach (var (name, purpose, cpuBudget, totalBudget) in profiles)
            {
                var pc = await BuildPcAsync(name, purpose, cpuBudget, totalBudget);
                if (pc != null) _db.PrebuiltPcs.Add(pc);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Prebuilt PCs seeded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to seed prebuilt PCs — will retry next startup.");
        }
    }

    private async Task<PrebuiltPc?> BuildPcAsync(string name, string purpose, decimal cpuBudget, decimal totalBudget)
    {
        var gpuBudget     = purpose is "Office" ? 0m : totalBudget * 0.35m;
        var mbBudget      = totalBudget * 0.10m;
        var ramBudget     = totalBudget * 0.07m;
        var storageBudget = totalBudget * 0.08m;
        var psuBudget     = totalBudget * 0.06m;
        var caseBudget    = totalBudget * 0.06m;
        var coolerBudget  = totalBudget * 0.05m;

        var cpu = await _db.Cpus
            .Where(x => x.Price > 0 && x.Price <= cpuBudget)
            .OrderByDescending(x => x.ApproximatePerformance)
            .FirstOrDefaultAsync();

        var mb = await _db.Motherboards
            .Where(x => x.Price > 0 && x.Price <= mbBudget)
            .OrderByDescending(x => x.Price)
            .FirstOrDefaultAsync();

        var ram = await _db.Memories
            .Where(x => x.Price > 0 && x.Price <= ramBudget)
            .OrderByDescending(x => x.Price)
            .FirstOrDefaultAsync();

        VideoCard? gpu = null;
        if (gpuBudget > 0)
            gpu = await _db.VideoCards
                .Where(x => x.Price > 0 && x.Price <= gpuBudget)
                .OrderByDescending(x => x.ApproximatePerformance)
                .FirstOrDefaultAsync();

        var storage = await _db.Storages
            .Where(x => x.Price > 0 && x.Price <= storageBudget)
            .OrderByDescending(x => x.Price)
            .FirstOrDefaultAsync();

        var psu = await _db.PowerSupplies
            .Where(x => x.Price > 0 && x.Price <= psuBudget)
            .OrderByDescending(x => x.Wattage)
            .FirstOrDefaultAsync();

        var caseEnc = await _db.CaseEnclosures
            .Where(x => x.Price > 0 && x.Price <= caseBudget)
            .OrderByDescending(x => x.Price)
            .FirstOrDefaultAsync();

        var cooler = await _db.CpuCoolers
            .Where(x => x.Price > 0 && x.Price <= coolerBudget)
            .OrderByDescending(x => x.Price)
            .FirstOrDefaultAsync();

        if (cpu == null && gpu == null) return null;

        var total = (cpu?.Price ?? 0) + (mb?.Price ?? 0) + (ram?.Price ?? 0)
                  + (gpu?.Price ?? 0) + (storage?.Price ?? 0) + (psu?.Price ?? 0)
                  + (caseEnc?.Price ?? 0) + (cooler?.Price ?? 0);

        return new PrebuiltPc
        {
            Name          = name,
            Purpose       = purpose,
            Price         = total,
            OldPrice      = total > 0 ? Math.Round(total * 1.08m, -3) : null,
            CpuId         = cpu?.Id,
            MotherboardId = mb?.Id,
            MemoryId      = ram?.Id,
            VideoCardId   = gpu?.Id,
            StorageId     = storage?.Id,
            PowerSupplyId = psu?.Id,
            CaseId        = caseEnc?.Id,
            CoolerId      = cooler?.Id,
        };
    }
}
