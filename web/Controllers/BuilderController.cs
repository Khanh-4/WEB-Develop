using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechSpecs.Data;
using TechSpecs.Services;
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
