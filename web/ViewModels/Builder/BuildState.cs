namespace TechSpecs.ViewModels.Builder;

/// <summary>
/// Sent from frontend via AJAX on every component selection change.
/// </summary>
public class BuildState
{
    public int? SelectedCpuId { get; set; }
    public int? SelectedMotherboardId { get; set; }
    public int? SelectedMemoryId { get; set; }
    public int? SelectedVideoCardId { get; set; }
    public int? SelectedPowerSupplyId { get; set; }
    public int? SelectedCaseId { get; set; }
    public int? SelectedStorageId { get; set; }
    public int? SelectedCoolerId { get; set; }

    // Pass 1 inputs
    public decimal? MaxBudget { get; set; }
    public string? SearchQuery { get; set; }
    public string? PreferredBrand { get; set; }

    // AI chatbot mode: override Pass 1 with AI-derived params
    public int? MinCpuPerformance { get; set; }
    public int? MinGpuPerformance { get; set; }
    public int? MinRamGb { get; set; }
}
