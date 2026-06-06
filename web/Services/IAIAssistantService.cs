namespace TechSpecs.Services;

public record AiBuildParams(
    int MinCpuPerformance,
    int MinGpuPerformance,
    int MinRamGb,
    decimal MaxBudget,
    string UseCase
);

public interface IAIAssistantService
{
    Task<AiBuildParams?> ParseBuildRequestAsync(string userMessage);
}
