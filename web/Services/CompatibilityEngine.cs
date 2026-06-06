using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.ViewModels.Builder;

namespace TechSpecs.Services;

public class CompatibilityEngine : ICompatibilityEngine
{
    private readonly AppDbContext _db;
    private static readonly Random _rng = new();
    private const int MaxResults = 30;

    public CompatibilityEngine(AppDbContext db) => _db = db;

    public async Task<FilteredResult> FilterAsync(BuildState state)
    {
        // Load selected components once for constraint checks
        var selCpu = state.SelectedCpuId.HasValue
            ? await _db.Cpus.FindAsync(state.SelectedCpuId.Value) : null;
        var selMb = state.SelectedMotherboardId.HasValue
            ? await _db.Motherboards.FindAsync(state.SelectedMotherboardId.Value) : null;
        var selMem = state.SelectedMemoryId.HasValue
            ? await _db.Memories.FindAsync(state.SelectedMemoryId.Value) : null;
        var selGpu = state.SelectedVideoCardId.HasValue
            ? await _db.VideoCards.FindAsync(state.SelectedVideoCardId.Value) : null;
        var selCase = state.SelectedCaseId.HasValue
            ? await _db.CaseEnclosures.FindAsync(state.SelectedCaseId.Value) : null;
        var selPsu = state.SelectedPowerSupplyId.HasValue
            ? await _db.PowerSupplies.FindAsync(state.SelectedPowerSupplyId.Value) : null;
        var selCooler = state.SelectedCoolerId.HasValue
            ? await _db.CpuCoolers.FindAsync(state.SelectedCoolerId.Value) : null;

        var result = new FilteredResult();

        result.Cpus         = await FilterCpusAsync(state, selMb, selGpu);
        result.Motherboards = await FilterMotherboardsAsync(state, selCpu, selMem, selCase);
        result.Memories     = await FilterMemoryAsync(state, selMb, selCpu);
        result.VideoCards   = await FilterVideoCardsAsync(state, selCase, selCpu, selPsu);
        result.PowerSupplies = await FilterPsuAsync(state, selCpu, selGpu, selCase);
        result.Cases        = await FilterCasesAsync(state, selMb, selGpu, selCooler);
        result.Storages     = await FilterStorageAsync(state, selMb);
        result.Coolers      = await FilterCoolersAsync(state, selCpu, selCase);

        // Build totals for the selected components
        result.TotalPrice = new[] {
            selCpu?.Price ?? 0, selMb?.Price ?? 0, selMem?.Price ?? 0,
            selGpu?.Price ?? 0, selCase?.Price ?? 0,
            selPsu?.Price ?? 0,
            state.SelectedStorageId.HasValue    ? (await _db.Storages.FindAsync(state.SelectedStorageId.Value))?.Price ?? 0 : 0,
            selCooler?.Price ?? 0,
        }.Sum();

        int cpuTdp = selCpu?.TDP ?? 0;
        int gpuTdp = selGpu?.TDP ?? 0;
        result.TotalTDP = cpuTdp + gpuTdp;
        result.RecommendedPsuWattage = (int)Math.Ceiling((cpuTdp + gpuTdp) * 1.3);

        return result;
    }

    private IQueryable<T> ApplyPass1<T>(IQueryable<T> q, BuildState state) where T : class
    {
        if (state.MaxBudget.HasValue)
            q = q.Where(e => EF.Property<decimal>(e, "Price") <= state.MaxBudget.Value);

        if (!string.IsNullOrWhiteSpace(state.SearchQuery))
        {
            var term = $"%{state.SearchQuery.Trim()}%";
            q = q.Where(e => EF.Functions.ILike(EF.Property<string>(e, "Name"), term));
        }

        if (!string.IsNullOrWhiteSpace(state.PreferredBrand))
        {
            var brand = $"%{state.PreferredBrand.Trim()}%";
            q = q.Where(e => EF.Functions.ILike(EF.Property<string>(e, "Manufacturer"), brand));
        }
        return q;
    }

    private static List<ComponentDto> ScoreAndSelectDtos(List<ComponentDto> dtos)
    {
        var valid = dtos.Where(x => x.Price > 0).ToList();

        var sorted = valid
            .OrderByDescending(x => x.IsCompatible)
            .ThenByDescending(x => x.IsRecommended)
            .ThenByDescending(x => x.PpScore)
            .ToList();

        var topCompat = sorted.Where(x => x.IsCompatible).Take(5).ToList();
        if (topCompat.Count >= 5)
        {
            var shuffled = topCompat.OrderBy(_ => _rng.Next()).ToList();
            for (int i = 0; i < 5; i++) sorted[i] = shuffled[i];
        }

        return sorted.Take(MaxResults).ToList();
    }

    private async Task<List<ComponentDto>> FilterCpusAsync(BuildState state, Motherboard? selMb, VideoCard? selGpu)
    {
        var q = ApplyPass1(_db.Cpus.AsQueryable(), state);
        if (state.MinCpuPerformance.HasValue) q = q.Where(c => c.ApproximatePerformance >= state.MinCpuPerformance.Value);

        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var c in list)
        {
            var dto = new ComponentDto
            {
                Id = c.Id, Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Socket"] = c.Socket, ["Cores"] = $"{c.CoreCount}C/{c.ThreadCount}T", ["Clock"] = $"{c.BaseClock}/{c.BoostClock} GHz", ["TDP"] = $"{c.TDP}W" }
            };
            dto.Badges.Add(c.Socket);
            if (c.Name.Contains("Core i9") || c.Name.Contains("Ryzen 9")) dto.Badges.Add("Enthusiast");
            else if (c.Name.Contains("Core i7") || c.Name.Contains("Ryzen 7")) dto.Badges.Add("High-End");
            else if (c.Name.Contains("Core i5") || c.Name.Contains("Ryzen 5")) dto.Badges.Add("Mid-Range");

            if (selMb != null && c.Socket != selMb.SocketCompatibility && selMb.SocketCompatibility != "Unknown" && selMb.SocketCompatibility != "Universal")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Socket mismatch (MB requires {selMb.SocketCompatibility})";
            }

            if (dto.IsCompatible && selGpu != null && c.ApproximatePerformance > 100 && selGpu.ApproximatePerformance > 400)
                dto.IsRecommended = true; // Pair high end with high end

            dto.PpScore = c.Price > 0 ? Math.Round((double)c.ApproximatePerformance / (double)c.Price * 1_000_000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterMotherboardsAsync(BuildState state, Cpu? selCpu, Memory? selMem, CaseEnclosure? selCase)
    {
        var q = ApplyPass1(_db.Motherboards.AsQueryable(), state);
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var m in list)
        {
            var dto = new ComponentDto
            {
                Id = m.Id, Name = m.Name, Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                Specs = new() { ["Socket"] = m.SocketCompatibility, ["Form Factor"] = m.FormFactor, ["Memory"] = $"{m.MemoryCompatibility} ({m.MemorySlots} slots)" }
            };
            
            if (m.SocketCompatibility != "Unknown") dto.Badges.Add(m.SocketCompatibility);
            dto.Badges.Add(m.MemoryCompatibility);
            dto.Badges.Add(m.FormFactor);

            if (selCpu != null && m.SocketCompatibility != selCpu.Socket && m.SocketCompatibility != "Unknown" && m.SocketCompatibility != "Universal")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Socket mismatch (Requires {selCpu.Socket})";
            }
            else if (selMem != null && m.MemoryCompatibility != selMem.Type && m.MemoryCompatibility != "Unknown")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Memory mismatch (Requires {selMem.Type})";
            }
            else if (selCase != null && !selCase.FormFactorSupport.Contains(m.FormFactor) && m.FormFactor != "Unknown")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Case does not support {m.FormFactor}";
            }

            if (dto.IsCompatible && selCpu != null)
            {
                if (selCpu.Name.Contains("K") && (m.Name.Contains("Z790") || m.Name.Contains("Z690"))) dto.IsRecommended = true;
                if (selCpu.Name.Contains("X") && (m.Name.Contains("X670") || m.Name.Contains("X570"))) dto.IsRecommended = true;
            }

            dto.PpScore = m.Price > 0 ? Math.Round(10_000_000_000d / (double)m.Price, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterMemoryAsync(BuildState state, Motherboard? selMb, Cpu? selCpu)
    {
        var q = ApplyPass1(_db.Memories.AsQueryable(), state);
        if (state.MinRamGb.HasValue) q = q.Where(m => m.Capacity >= state.MinRamGb.Value);
        
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var m in list)
        {
            var dto = new ComponentDto
            {
                Id = m.Id, Name = m.Name, Manufacturer = m.Manufacturer, Price = m.Price, ImageUrl = m.ImageUrl,
                Specs = new() { ["Type"] = m.Type, ["Capacity"] = $"{m.Capacity}GB ({m.Modules}x{m.Capacity / m.Modules}GB)", ["Speed"] = $"{m.Speed}MHz" }
            };
            dto.Badges.Add(m.Type);
            dto.Badges.Add($"{m.Capacity}GB");
            
            if (selMb != null && m.Type != selMb.MemoryCompatibility && selMb.MemoryCompatibility != "Unknown")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"MB only supports {selMb.MemoryCompatibility}";
            }

            if (dto.IsCompatible && selCpu != null)
            {
                if (m.Type == "DDR5" && m.Speed >= 6000) dto.IsRecommended = true;
                if (m.Type == "DDR4" && m.Speed >= 3200 && (selCpu.Name.Contains("Ryzen 5") || selCpu.Name.Contains("Core i5"))) dto.IsRecommended = true;
            }

            dto.PpScore = m.Price > 0 ? Math.Round((double)(m.Capacity * m.Speed) / (double)m.Price * 1000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterVideoCardsAsync(BuildState state, CaseEnclosure? selCase, Cpu? selCpu, PowerSupply? selPsu)
    {
        var q = ApplyPass1(_db.VideoCards.AsQueryable(), state);
        if (state.MinGpuPerformance.HasValue) q = q.Where(g => g.ApproximatePerformance >= state.MinGpuPerformance.Value);

        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var g in list)
        {
            var dto = new ComponentDto
            {
                Id = g.Id, Name = g.Name, Manufacturer = g.Manufacturer, Price = g.Price, ImageUrl = g.ImageUrl,
                Specs = new() { ["VRAM"] = $"{g.VRAM}GB", ["Length"] = $"{g.Length}mm", ["TDP"] = $"{g.TDP}W" }
            };

            if (g.Name.Contains("RTX 40") || g.Name.Contains("RX 7000")) dto.Badges.Add("Current Gen");
            else if (g.Name.Contains("RTX 30") || g.Name.Contains("RX 6000")) dto.Badges.Add("Prev Gen");

            if (selCase != null && selCase.MaxVGALength > 0 && g.Length > selCase.MaxVGALength)
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Too long for Case (Max {selCase.MaxVGALength}mm)";
            }
            else if (selPsu != null && selPsu.Wattage > 0)
            {
                int cpuTdp = selCpu?.TDP ?? 65; 
                if (selPsu.Wattage < (g.TDP + cpuTdp) * 1.2)
                {
                    dto.IsCompatible = false;
                    dto.IncompatibleReason = $"PSU Wattage too low";
                }
            }

            if (dto.IsCompatible && selCpu != null)
            {
                if (g.ApproximatePerformance > 400 && selCpu.ApproximatePerformance > 100) dto.IsRecommended = true;
                else if (g.ApproximatePerformance <= 400 && selCpu.ApproximatePerformance <= 100) dto.IsRecommended = true;
            }

            dto.PpScore = g.Price > 0 ? Math.Round((double)g.ApproximatePerformance / (double)g.Price * 1_000_000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterPsuAsync(BuildState state, Cpu? selCpu, VideoCard? selGpu, CaseEnclosure? selCase)
    {
        var q = ApplyPass1(_db.PowerSupplies.AsQueryable(), state);
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        
        int cpuTdp = selCpu?.TDP ?? 0;
        int gpuTdp = selGpu?.TDP ?? 0;
        int minWatts = (int)Math.Ceiling((cpuTdp + gpuTdp) * 1.3);

        foreach (var p in list)
        {
            var dto = new ComponentDto
            {
                Id = p.Id, Name = p.Name, Manufacturer = p.Manufacturer, Price = p.Price, ImageUrl = p.ImageUrl,
                Specs = new() { ["Wattage"] = $"{p.Wattage}W", ["Efficiency"] = p.Efficiency ?? "—", ["Modular"] = p.Modular ?? "—" }
            };
            
            if (!string.IsNullOrEmpty(p.Efficiency)) dto.Badges.Add(p.Efficiency);
            if (p.Wattage >= 800) dto.Badges.Add("High Power");

            if (minWatts > 0 && p.Wattage < minWatts)
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Wattage too low (requires {minWatts}W)";
            }

            if (dto.IsCompatible && p.Wattage >= minWatts && p.Wattage <= minWatts + 250)
            {
                if (p.Efficiency != null && (p.Efficiency.Contains("Gold") || p.Efficiency.Contains("Platinum")))
                    dto.IsRecommended = true;
            }

            dto.PpScore = p.Price > 0 ? Math.Round((double)p.Wattage / (double)p.Price * 100_000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterCasesAsync(BuildState state, Motherboard? selMb, VideoCard? selGpu, CpuCooler? selCooler)
    {
        var q = ApplyPass1(_db.CaseEnclosures.AsQueryable(), state);
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var c in list)
        {
            var dto = new ComponentDto
            {
                Id = c.Id, Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Form Factor"] = c.FormFactorSupport, ["Max GPU"] = $"{c.MaxVGALength}mm", ["Color"] = c.Color ?? "—" }
            };
            
            dto.Badges.Add(c.FormFactorSupport.Contains("ATX") ? "ATX" : "Compact");

            if (selMb != null && !c.FormFactorSupport.Contains(selMb.FormFactor) && selMb.FormFactor != "Unknown")
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"Does not support {selMb.FormFactor} MB";
            }
            else if (selGpu != null && selGpu.Length > 0 && c.MaxVGALength < selGpu.Length)
            {
                dto.IsCompatible = false;
                dto.IncompatibleReason = $"GPU too long (Max {c.MaxVGALength}mm)";
            }
            
            if (dto.IsCompatible && (c.Name.ToLower().Contains("mesh") || c.Name.ToLower().Contains("airflow")))
                dto.IsRecommended = true;

            dto.PpScore = c.Price > 0 ? Math.Round(10_000_000_000d / (double)c.Price, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterStorageAsync(BuildState state, Motherboard? selMb)
    {
        var q = ApplyPass1(_db.Storages.AsQueryable(), state);
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var s in list)
        {
            var dto = new ComponentDto
            {
                Id = s.Id, Name = s.Name, Manufacturer = s.Manufacturer, Price = s.Price, ImageUrl = s.ImageUrl,
                Specs = new() { ["Type"] = s.Type, ["Capacity"] = $"{s.Capacity}GB", ["Interface"] = s.Interface, ["Read"] = s.ReadSpeed > 0 ? $"{s.ReadSpeed}MB/s" : "—" }
            };
            
            dto.Badges.Add(s.Type); 
            if (s.Capacity >= 1000) dto.Badges.Add("1TB+");

            if (dto.IsCompatible && s.Type == "NVMe" && s.ReadSpeed >= 3500)
                dto.IsRecommended = true;

            dto.PpScore = s.Price > 0 ? Math.Round((double)(s.Capacity * (s.ReadSpeed > 0 ? s.ReadSpeed : 500)) / (double)s.Price * 1000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }

    private async Task<List<ComponentDto>> FilterCoolersAsync(BuildState state, Cpu? selCpu, CaseEnclosure? selCase)
    {
        var q = ApplyPass1(_db.CpuCoolers.AsQueryable(), state);
        var list = await q.ToListAsync();
        var dtos = new List<ComponentDto>();
        foreach (var c in list)
        {
            var dto = new ComponentDto
            {
                Id = c.Id, Name = c.Name, Manufacturer = c.Manufacturer, Price = c.Price, ImageUrl = c.ImageUrl,
                Specs = new() { ["Type"] = c.Type, ["Max TDP"] = $"{c.MaxTDP}W", ["Height"] = $"{c.Height}mm" }
            };
            
            dto.Badges.Add(c.Type.Contains("AIO") ? "Water Cooler" : "Air Cooler");

            if (selCpu != null)
            {
                if (!c.SocketCompatibility.Contains(selCpu.Socket) && c.SocketCompatibility != "Universal" && c.SocketCompatibility != "Unknown")
                {
                    dto.IsCompatible = false;
                    dto.IncompatibleReason = $"Does not support {selCpu.Socket}";
                }
                else if (selCpu.TDP > 0 && c.MaxTDP < selCpu.TDP)
                {
                    dto.IsCompatible = false;
                    dto.IncompatibleReason = $"Cooling capacity too low";
                }
            }

            if (dto.IsCompatible && selCpu != null)
            {
                if (selCpu.TDP >= 105 && c.Type.Contains("AIO")) dto.IsRecommended = true;
                if (selCpu.TDP < 105 && !c.Type.Contains("AIO")) dto.IsRecommended = true;
            }

            dto.PpScore = c.Price > 0 ? Math.Round((double)c.MaxTDP / (double)c.Price * 1_000_000, 4) : 0;
            dtos.Add(dto);
        }
        return ScoreAndSelectDtos(dtos);
    }
}
