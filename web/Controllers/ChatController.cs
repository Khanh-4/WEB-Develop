using Microsoft.AspNetCore.Mvc;
using TechSpecs.Services;
using TechSpecs.ViewModels.Builder;

namespace TechSpecs.Controllers;

public class ChatController : Controller
{
    private readonly IAIAssistantService _ai;
    private readonly ICompatibilityEngine _engine;

    public ChatController(IAIAssistantService ai, ICompatibilityEngine engine)
    {
        _ai = ai; _engine = engine;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest req)
    {
        if (string.IsNullOrWhiteSpace(req?.Message))
            return BadRequest(new { error = "Empty message" });

        var aiParams = await _ai.ParseBuildRequestAsync(req.Message);
        if (aiParams is null)
            return Json(new ChatResponse
            {
                Success = false,
                Reply = "Xin lỗi, mình chưa hiểu yêu cầu của bạn. Hãy mô tả ngân sách và nhu cầu sử dụng — ví dụ: \"Mình có 20 triệu để chơi game\" hoặc \"Build PC 35 triệu làm đồ họa\"."
            });

        // Run engine without budget cap to get full filtered candidate pool
        var buildState = new BuildState
        {
            MinCpuPerformance = aiParams.MinCpuPerformance > 0 ? aiParams.MinCpuPerformance : null,
            MinGpuPerformance = aiParams.MinGpuPerformance > 0 ? aiParams.MinGpuPerformance : null,
            MinRamGb          = aiParams.MinRamGb > 0 ? aiParams.MinRamGb : null,
        };
        var result = await _engine.FilterAsync(buildState);

        // Allocate budget per component based on use-case priority
        var alloc = GetBudgetAllocation(aiParams.UseCase, aiParams.MaxBudget);

        var cpu         = PickBest(result.Cpus,          alloc["cpu"],     aiParams.MaxBudget);
        var motherboard = PickBest(result.Motherboards,   alloc["mb"],      aiParams.MaxBudget);
        var memory      = PickBest(result.Memories,       alloc["ram"],     aiParams.MaxBudget);
        var gpu         = PickBest(result.VideoCards,     alloc["gpu"],     aiParams.MaxBudget);
        var storage     = PickBest(result.Storages,       alloc["storage"], aiParams.MaxBudget);
        var psu         = PickBest(result.PowerSupplies,  alloc["psu"],     aiParams.MaxBudget);
        var cas         = PickBest(result.Cases,          alloc["case"],    aiParams.MaxBudget);
        var cooler      = PickBest(result.Coolers,        alloc["cooler"],  aiParams.MaxBudget);

        var totalPrice = new[] { cpu, motherboard, memory, gpu, storage, psu, cas, cooler }
            .Where(x => x is not null)
            .Sum(x => x!.Price);

        var build = new
        {
            cpu, motherboard, memory, gpu, storage,
            powerSupply = psu,
            @case = cas,
            cooler,
        };

        return Json(new ChatResponse
        {
            Success    = true,
            Reply      = BuildReplyMessage(aiParams.UseCase, totalPrice, aiParams.MaxBudget),
            Build      = build,
            TotalPrice = totalPrice,
            UseCase    = aiParams.UseCase,
        });
    }

    // ── Budget allocation ratios per use-case ─────────────────────────────────
    // Keys: cpu, gpu, mb, ram, storage, psu, case, cooler

    private static Dictionary<string, decimal> GetBudgetAllocation(string useCase, decimal budget)
    {
        // Ratios must sum to 1.0
        var ratios = useCase switch
        {
            "gaming"    => new[] { 0.24m, 0.33m, 0.11m, 0.09m, 0.08m, 0.07m, 0.05m, 0.03m },
            "design"    => new[] { 0.28m, 0.18m, 0.11m, 0.17m, 0.10m, 0.07m, 0.06m, 0.03m },
            "streaming" => new[] { 0.26m, 0.28m, 0.11m, 0.10m, 0.08m, 0.07m, 0.07m, 0.03m },
            "office"    => new[] { 0.30m, 0.00m, 0.12m, 0.16m, 0.17m, 0.09m, 0.11m, 0.05m },
            _           => new[] { 0.26m, 0.24m, 0.11m, 0.12m, 0.09m, 0.08m, 0.07m, 0.03m },
        };
        var keys = new[] { "cpu", "gpu", "mb", "ram", "storage", "psu", "case", "cooler" };
        return keys.Zip(ratios, (k, r) => (k, v: budget * r))
                   .ToDictionary(x => x.k, x => x.v);
    }

    /// Pick the strongest component that fits the allocated budget.
    /// We want to *use* the budget the customer asked for, not under-spend on a
    /// cheap high-value part — so within the allocation ceiling we pick the part
    /// with the highest estimated performance (PpScore × Price ≈ raw performance),
    /// not the highest value ratio. Falls back to cheapest if nothing fits.
    private static ComponentDto? PickBest(List<ComponentDto> items, decimal allocated, decimal totalBudget)
    {
        if (!items.Any()) return null;

        // Prefer items within 140% of allocation (some flex room to use the budget)
        var ceiling = allocated * 1.4m;
        var inBudget = items.Where(x => x.Price <= ceiling && x.Price > 0)
                            .OrderByDescending(x => x.PpScore * (double)x.Price) // ≈ absolute performance
                            .ThenByDescending(x => x.Price)
                            .ToList();

        if (inBudget.Any()) return inBudget.First();

        // Fallback: cheapest available
        return items.Where(x => x.Price > 0)
                    .OrderBy(x => x.Price)
                    .FirstOrDefault()
               ?? items.First();
    }

    private static string BuildReplyMessage(string useCase, decimal total, decimal budget)
    {
        var fmtTotal  = total.ToString("N0") + "đ";
        var fmtBudget = budget > 0 ? budget.ToString("N0") + "đ" : "ngân sách của bạn";
        var withinBudget = budget > 0 && total <= budget * 1.05m;
        var budgetNote = withinBudget ? "" : $" *(nhỉnh hơn {fmtBudget} một chút — bạn có thể chỉnh lại linh kiện cho phù hợp)*";

        return useCase switch
        {
            "gaming"    => $"Đây là cấu hình chơi game — tổng **{fmtTotal}**{budgetNote}. Ưu tiên GPU mạnh để chơi mượt.",
            "design"    => $"Đây là cấu hình đồ họa/render — tổng **{fmtTotal}**{budgetNote}. Ưu tiên CPU nhiều nhân và RAM lớn.",
            "streaming" => $"Đây là cấu hình stream — tổng **{fmtTotal}**{budgetNote}. Cân bằng giữa CPU và GPU.",
            "office"    => $"Đây là cấu hình văn phòng — tổng **{fmtTotal}**{budgetNote}. Gọn nhẹ và tiết kiệm điện.",
            _           => $"Đây là cấu hình cân bằng — tổng **{fmtTotal}**{budgetNote}.",
        };
    }
}

public class ChatRequest  { public string? Message { get; set; } }
public class ChatResponse
{
    public bool Success    { get; set; }
    public string Reply    { get; set; } = string.Empty;
    public object? Build   { get; set; }
    public decimal TotalPrice { get; set; }
    public string UseCase  { get; set; } = string.Empty;
}
