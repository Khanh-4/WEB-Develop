using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Services;
using TechSpecs.ViewModels;
using TechSpecs.ViewModels.Builder;

namespace TechSpecs.Controllers;

public class BuilderController : Controller
{
    private readonly ICompatibilityEngine _engine;
    private readonly AppDbContext _db;

    public BuilderController(ICompatibilityEngine engine, AppDbContext db)
    {
        _engine = engine;
        _db = db;
    }

    // GET /Builder — main PC builder page (loads with empty state)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var initialResult = await _engine.FilterAsync(new BuildState());
        return View(initialResult);
    }

    // POST /Builder/Filter — AJAX endpoint called on every component selection
    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] BuildState state)
    {
        if (state is null)
            return BadRequest();

        var result = await _engine.FilterAsync(state);
        return Json(result);
    }

    // POST /Builder/CompareBuilds — real-time build comparison with radar scores + benchmarks
    [HttpPost]
    public async Task<IActionResult> CompareBuilds([FromBody] CompareRequest req)
    {
        if (req is null) return BadRequest();
        var result = new BuildComparisonResult
        {
            A = await BuildSnapshotAsync(req.BuildA),
            B = await BuildSnapshotAsync(req.BuildB),
        };
        return Json(result);
    }

    private async Task<BuildSnapshot> BuildSnapshotAsync(BuildState state)
    {
        var cpu  = state.SelectedCpuId.HasValue       ? await _db.Cpus.FindAsync(state.SelectedCpuId.Value)            : null;
        var mb   = state.SelectedMotherboardId.HasValue ? await _db.Motherboards.FindAsync(state.SelectedMotherboardId.Value) : null;
        var mem  = state.SelectedMemoryId.HasValue     ? await _db.Memories.FindAsync(state.SelectedMemoryId.Value)     : null;
        var gpu  = state.SelectedVideoCardId.HasValue  ? await _db.VideoCards.FindAsync(state.SelectedVideoCardId.Value) : null;
        var stor = state.SelectedStorageId.HasValue    ? await _db.Storages.FindAsync(state.SelectedStorageId.Value)    : null;
        var psu  = state.SelectedPowerSupplyId.HasValue? await _db.PowerSupplies.FindAsync(state.SelectedPowerSupplyId.Value) : null;
        var cas  = state.SelectedCaseId.HasValue       ? await _db.CaseEnclosures.FindAsync(state.SelectedCaseId.Value) : null;
        var cool = state.SelectedCoolerId.HasValue     ? await _db.CpuCoolers.FindAsync(state.SelectedCoolerId.Value)   : null;

        decimal totalPrice = (cpu?.Price ?? 0) + (mb?.Price ?? 0) + (mem?.Price ?? 0)
                           + (gpu?.Price ?? 0) + (stor?.Price ?? 0) + (psu?.Price ?? 0)
                           + (cas?.Price ?? 0) + (cool?.Price ?? 0);
        int cpuTdp = cpu?.TDP ?? 0;
        int gpuTdp = gpu?.TDP ?? 0;
        int totalTdp = cpuTdp + gpuTdp;

        // ── Radar Scores ─────────────────────────────────────────────
        int gaming = gpu != null ? (int)Math.Min(100, (double)gpu.ApproximatePerformance / 9.0) : 0;

        int multitasking = cpu != null
            ? (int)Math.Min(100, cpu.CoreCount * 3.0 + (double)cpu.BoostClock * 7.0)
            : 0;

        int storage = stor != null
            ? stor.ReadSpeed > 5000 ? 95
            : stor.ReadSpeed > 3000 ? 80
            : stor.ReadSpeed > 500  ? 60
            : stor.ReadSpeed > 0    ? 40
            : 30
            : 0;

        int thermal = 50; // default: assume stock cooling
        if (cool != null && totalTdp > 0)
            thermal = (int)Math.Min(100, cool.MaxTDP * 100.0 / totalTdp);
        else if (cool != null)
            thermal = Math.Min(100, cool.MaxTDP / 2);

        int psuHeadroomW   = psu != null ? Math.Max(0, psu.Wattage - totalTdp - 50) : 0;
        int psuHeadroomPct = psu != null && psu.Wattage > 0 ? (int)(psuHeadroomW * 100.0 / psu.Wattage) : 0;
        int ramFreeSlots   = mb != null ? Math.Max(0, mb.MemorySlots - 2) : 0;
        bool isDdr5        = mb?.MemoryCompatibility?.Contains("DDR5", StringComparison.OrdinalIgnoreCase) == true;
        int upgrade = (int)Math.Min(100,
            psuHeadroomPct * 0.5 + ramFreeSlots * 15 + (isDdr5 ? 20 : 10));

        // ── Benchmark: try DB first, fallback ApproxPerf ─────────────
        BenchmarkData? benchmark = null;
        bool isReal = false;

        // GPU benchmarks
        if (gpu != null)
        {
            var gpuBench = await _db.ComponentBenchmarks
                .Where(b => b.Category == "gpu" &&
                            EF.Functions.ILike(gpu.Name, $"%{b.ComponentName}%"))
                .OrderByDescending(b => b.Id).FirstOrDefaultAsync();

            if (gpuBench != null && (gpuBench.FpsCs2_1080p.HasValue || gpuBench.FpsCyberpunk_1080p.HasValue))
            {
                benchmark = new BenchmarkData
                {
                    FpsCs2_1080p       = gpuBench.FpsCs2_1080p,
                    FpsCs2_1440p       = gpuBench.FpsCs2_1440p,
                    FpsCyberpunk_1080p = gpuBench.FpsCyberpunk_1080p,
                    FpsCyberpunk_1440p = gpuBench.FpsCyberpunk_1440p,
                    FpsValorant_1080p  = gpuBench.FpsValorant_1080p,
                    FpsValorant_1440p  = gpuBench.FpsValorant_1440p,
                };
                isReal = true;
            }
            else
            {
                // Fallback: estimate from ApproxPerf tier
                benchmark = EstimateGpuBenchmark(gpu.ApproximatePerformance);
            }
        }

        // CPU benchmarks (merge with existing benchmark)
        if (cpu != null)
        {
            var cpuBench = await _db.ComponentBenchmarks
                .Where(b => b.Category == "cpu" &&
                            EF.Functions.ILike(cpu.Name, $"%{b.ComponentName}%"))
                .OrderByDescending(b => b.Id).FirstOrDefaultAsync();

            if (cpuBench != null && cpuBench.CinebenchR23Multi.HasValue)
            {
                benchmark ??= new BenchmarkData();
                benchmark.CinebenchMulti  = cpuBench.CinebenchR23Multi;
                benchmark.CinebenchSingle = cpuBench.CinebenchR23Single;
                isReal = true;
            }
            else
            {
                benchmark ??= new BenchmarkData();
                benchmark.CinebenchMulti  = EstimateCinebench(cpu);
                benchmark.CinebenchSingle = (int)(cpu.BoostClock * 280);
            }
        }

        // ── Specs Detail ─────────────────────────────────────────────
        var specs = new BuildSpecsDetail
        {
            Cpu = cpu == null ? null : new ComponentSnap
            {
                Name = cpu.Name, Price = cpu.Price,
                KeyStats = new() {
                    ["Nhân/Luồng"] = $"{cpu.CoreCount}C / {cpu.ThreadCount}T",
                    ["Xung tăng"]  = $"{cpu.BoostClock} GHz",
                    ["TDP"]        = $"{cpu.TDP} W",
                    ["Socket"]     = cpu.Socket,
                }
            },
            Gpu = gpu == null ? null : new ComponentSnap
            {
                Name = gpu.Name, Price = gpu.Price,
                KeyStats = new() {
                    ["VRAM"] = $"{gpu.VRAM} GB",
                    ["TDP"]  = $"{gpu.TDP} W",
                }
            },
            Memory = mem == null ? null : new ComponentSnap
            {
                Name = mem.Name, Price = mem.Price,
                KeyStats = new() {
                    ["Loại"]       = mem.Type,
                    ["Dung lượng"] = $"{mem.Capacity} GB",
                    ["Tốc độ"]     = $"{mem.Speed} MHz",
                }
            },
            Storage = stor == null ? null : new ComponentSnap
            {
                Name = stor.Name, Price = stor.Price,
                KeyStats = new() {
                    ["Loại"]      = stor.Type,
                    ["Dung lượng"]= $"{stor.Capacity} GB",
                    ["Đọc"]       = stor.ReadSpeed > 0 ? $"{stor.ReadSpeed} MB/s" : "—",
                    ["Ghi"]       = stor.WriteSpeed > 0 ? $"{stor.WriteSpeed} MB/s" : "—",
                }
            },
            Psu = psu == null ? null : new ComponentSnap
            {
                Name = psu.Name, Price = psu.Price,
                KeyStats = new() {
                    ["Công suất"] = $"{psu.Wattage} W",
                    ["Hiệu suất"] = psu.Efficiency,
                    ["Kiểu dây"]  = psu.Modular,
                }
            },
            Motherboard = mb == null ? null : new ComponentSnap
            {
                Name = mb.Name, Price = mb.Price,
                KeyStats = new() {
                    ["Socket"]     = mb.SocketCompatibility,
                    ["Chipset"]    = mb.Chipset,
                    ["Form Factor"]= mb.FormFactor,
                    ["RAM"]        = mb.MemoryCompatibility,
                    ["Khe RAM"]    = mb.MemorySlots.ToString(),
                }
            },
            Cooler = cool == null ? null : new ComponentSnap
            {
                Name = cool.Name, Price = cool.Price,
                KeyStats = new() {
                    ["Loại"]    = cool.Type,
                    ["Max TDP"] = $"{cool.MaxTDP} W",
                }
            },
            Case = cas == null ? null : new ComponentSnap
            {
                Name = cas.Name, Price = cas.Price,
                KeyStats = new() {
                    ["Loại"]   = cas.CaseType ?? "—",
                    ["Hỗ trợ"] = cas.FormFactorSupport,
                    ["Màu"]    = cas.Color ?? "—",
                }
            },
            PsuHeadroomW   = psuHeadroomW,
            PsuHeadroomPct = psuHeadroomPct,
            RamFreeSlots   = ramFreeSlots,
            CoolerType     = cool?.Type ?? "—",
            CaseFormFactor = cas?.FormFactorSupport ?? "—",
        };

        return new BuildSnapshot
        {
            TotalPrice      = totalPrice,
            TotalTDP        = totalTdp,
            Radar           = new RadarScores { Gaming = gaming, Multitasking = multitasking, Storage = storage, Thermal = thermal, Upgrade = upgrade },
            Specs           = specs,
            Benchmark       = benchmark,
            BenchmarkIsReal = isReal,
        };
    }

    private static BenchmarkData EstimateGpuBenchmark(decimal approxPerf)
    {
        // Tier lookup from P32 FPS Estimator tables
        (decimal minPerf, int cs2_1080, int cs2_1440, int cp_1080, int cp_1440, int val_1080, int val_1440)[] tiers =
        [
            (900, 350, 280, 100, 70, 450, 350),
            (700, 270, 210, 80, 57, 375, 285),
            (500, 210, 155, 57, 42, 315, 235),
            (350, 160, 112, 44, 32, 240, 180),
            (200, 125, 86,  33, 23, 185, 135),
            (100, 95,  62,  23, 14, 140, 95),
            (0,   67,  42,  14, 7,  100, 65),
        ];
        foreach (var t in tiers)
            if (approxPerf >= t.minPerf)
                return new BenchmarkData
                {
                    FpsCs2_1080p       = t.cs2_1080,
                    FpsCs2_1440p       = t.cs2_1440,
                    FpsCyberpunk_1080p = t.cp_1080,
                    FpsCyberpunk_1440p = t.cp_1440,
                    FpsValorant_1080p  = t.val_1080,
                    FpsValorant_1440p  = t.val_1440,
                    EstimatedGamingScore = (int)Math.Min(100, (double)approxPerf / 9.0),
                };
        return new BenchmarkData { EstimatedGamingScore = 0 };
    }

    private static int EstimateCinebench(Cpu cpu)
    {
        // Rough Cinebench R23 multi-core estimate: cores × boostClock × ~280
        return (int)(cpu.CoreCount * (double)cpu.BoostClock * 280);
    }

    // POST /Builder/ExportPdf — generate quotation PDF from selected components
    [HttpPost]
    public async Task<IActionResult> ExportPdf([FromBody] BuildState state)
    {
        if (state is null) return BadRequest();

        var items = new List<(string Cat, string Name, decimal Price)>();

        if (state.SelectedCpuId.HasValue)
        {
            var x = await _db.Cpus.AsNoTracking()
                .Where(c => c.Id == state.SelectedCpuId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("CPU", x.Name, x.Price));
        }
        if (state.SelectedMotherboardId.HasValue)
        {
            var x = await _db.Motherboards.AsNoTracking()
                .Where(c => c.Id == state.SelectedMotherboardId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("Mainboard", x.Name, x.Price));
        }
        if (state.SelectedMemoryId.HasValue)
        {
            var x = await _db.Memories.AsNoTracking()
                .Where(c => c.Id == state.SelectedMemoryId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("RAM", x.Name, x.Price));
        }
        if (state.SelectedVideoCardId.HasValue)
        {
            var x = await _db.VideoCards.AsNoTracking()
                .Where(c => c.Id == state.SelectedVideoCardId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("GPU", x.Name, x.Price));
        }
        if (state.SelectedStorageId.HasValue)
        {
            var x = await _db.Storages.AsNoTracking()
                .Where(c => c.Id == state.SelectedStorageId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("Storage", x.Name, x.Price));
        }
        if (state.SelectedPowerSupplyId.HasValue)
        {
            var x = await _db.PowerSupplies.AsNoTracking()
                .Where(c => c.Id == state.SelectedPowerSupplyId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("PSU", x.Name, x.Price));
        }
        if (state.SelectedCaseId.HasValue)
        {
            var x = await _db.CaseEnclosures.AsNoTracking()
                .Where(c => c.Id == state.SelectedCaseId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("Case", x.Name, x.Price));
        }
        if (state.SelectedCoolerId.HasValue)
        {
            var x = await _db.CpuCoolers.AsNoTracking()
                .Where(c => c.Id == state.SelectedCoolerId.Value)
                .Select(c => new { c.Name, c.Price }).FirstOrDefaultAsync();
            if (x != null) items.Add(("Tản nhiệt", x.Name, x.Price));
        }

        if (items.Count == 0)
            return BadRequest("Chưa chọn linh kiện nào.");

        var pdfBytes = GenerateQuotationPdf(items);
        var filename = $"TechSpecs-BaoGia-{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    private static byte[] GenerateQuotationPdf(List<(string Cat, string Name, decimal Price)> items)
    {
        var purple = Color.FromHex("#7C3AED");
        var lightPurple = Color.FromHex("#EDE9FE");
        var gray50 = Color.FromHex("#F9FAFB");
        var gray100 = Color.FromHex("#F3F4F6");
        var gray600 = Color.FromHex("#4B5563");
        var total = items.Sum(x => x.Price);
        var dateStr = DateTime.Now.ToString("dd/MM/yyyy");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(40);
                page.MarginVertical(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#111827"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("TECHSPECS")
                                .Bold().FontSize(24).FontColor(purple);
                            c.Item().Text("BÁO GIÁ CẤU HÌNH PC")
                                .SemiBold().FontSize(13).FontColor(gray600);
                            c.Item().PaddingTop(2).Text($"Ngày lập: {dateStr}")
                                .FontSize(9).FontColor(gray600);
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text("techspecs.vn")
                                .FontSize(9).FontColor(gray600);
                            c.Item().AlignRight().Text("contact@techspecs.vn")
                                .FontSize(9).FontColor(gray600);
                        });
                    });
                    col.Item().PaddingTop(12).LineHorizontal(1.5f).LineColor(purple);
                });

                page.Content().PaddingTop(20).Column(col =>
                {
                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);
                            cols.ConstantColumn(85);
                            cols.RelativeColumn();
                            cols.ConstantColumn(115);
                        });

                        // Header
                        table.Header(h =>
                        {
                            static IContainer HeaderCell(IContainer c) =>
                                c.Background(Color.FromHex("#7C3AED"))
                                 .PaddingVertical(8).PaddingHorizontal(10);

                            HeaderCell(h.Cell()).Text("#").Bold().FontColor(Colors.White);
                            HeaderCell(h.Cell()).Text("Loại").Bold().FontColor(Colors.White);
                            HeaderCell(h.Cell()).Text("Tên sản phẩm").Bold().FontColor(Colors.White);
                            HeaderCell(h.Cell()).AlignRight().Text("Đơn giá").Bold().FontColor(Colors.White);
                        });

                        // Rows
                        for (int i = 0; i < items.Count; i++)
                        {
                            var (cat, name, price) = items[i];
                            var bg = i % 2 == 0 ? gray50 : Colors.White;

                            static IContainer DataCell(IContainer c, Color bg) =>
                                c.Background(bg).BorderBottom(0.5f).BorderColor(Color.FromHex("#E5E7EB"))
                                 .PaddingVertical(9).PaddingHorizontal(10);

                            DataCell(table.Cell(), bg).Text($"{i + 1}").FontColor(gray600);
                            DataCell(table.Cell(), bg).Text(cat).SemiBold();
                            DataCell(table.Cell(), bg).Text(name);
                            DataCell(table.Cell(), bg).AlignRight()
                                .Text($"{price:N0}đ").SemiBold().FontColor(purple);
                        }

                        // Total row
                        table.Cell().ColumnSpan(3)
                            .Background(gray100).PaddingVertical(10).PaddingHorizontal(10)
                            .AlignRight().Text("TỔNG CỘNG").Bold().FontSize(11);
                        table.Cell()
                            .Background(gray100).PaddingVertical(10).PaddingHorizontal(10)
                            .AlignRight().Text($"{total:N0}đ").Bold().FontSize(12).FontColor(purple);
                    });

                    col.Item().PaddingTop(24).Column(notes =>
                    {
                        notes.Item().Text("Lưu ý:").SemiBold().FontSize(9).FontColor(gray600);
                        foreach (var line in new[] {
                            "• Giá trên đã bao gồm VAT.",
                            "• Báo giá có hiệu lực trong 7 ngày kể từ ngày lập.",
                            "• Liên hệ hotline hoặc chat để được tư vấn thêm.",
                        })
                        {
                            notes.Item().Text(line).FontSize(9).FontColor(gray600);
                        }
                    });
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().AlignMiddle().Text(text =>
                    {
                        text.Span("Trang ").FontSize(8).FontColor(gray600);
                        text.CurrentPageNumber().FontSize(8).FontColor(gray600);
                        text.Span(" / ").FontSize(8).FontColor(gray600);
                        text.TotalPages().FontSize(8).FontColor(gray600);
                    });
                    row.RelativeItem().AlignRight().AlignMiddle()
                        .Text("Cảm ơn quý khách đã tin tưởng TechSpecs!")
                        .FontSize(8).FontColor(gray600);
                });
            });
        }).GeneratePdf();
    }
}
