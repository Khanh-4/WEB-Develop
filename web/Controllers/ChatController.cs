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
                Reply = "Sorry, I couldn't understand your request. Please describe your budget and use-case (e.g. 'I have 20 million VND for gaming')."
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

    /// Pick the highest P/P component within the allocated budget.
    /// Falls back to cheapest available if nothing fits the allocation.
    private static ComponentDto? PickBest(List<ComponentDto> items, decimal allocated, decimal totalBudget)
    {
        if (!items.Any()) return null;

        // Prefer items within 130% of allocation (some flex room)
        var ceiling = allocated * 1.3m;
        var inBudget = items.Where(x => x.Price <= ceiling && x.Price > 0)
                            .OrderByDescending(x => x.PpScore)
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
        var fmtBudget = budget > 0 ? budget.ToString("N0") + "đ" : "your budget";
        var withinBudget = budget > 0 && total <= budget * 1.05m;
        var budgetNote = withinBudget ? "" : $" *(slightly over {fmtBudget} — adjust components as needed)*";

        return useCase switch
        {
            "gaming"    => $"Here's a gaming build — total **{fmtTotal}**{budgetNote}. GPU prioritized for smooth framerates.",
            "design"    => $"Here's a design/render build — total **{fmtTotal}**{budgetNote}. CPU cores and RAM prioritized.",
            "streaming" => $"Here's a streaming build — total **{fmtTotal}**{budgetNote}. Balanced CPU + GPU.",
            "office"    => $"Here's an office build — total **{fmtTotal}**{budgetNote}. Efficient and compact.",
            _           => $"Here's a balanced build — total **{fmtTotal}**{budgetNote}.",
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
