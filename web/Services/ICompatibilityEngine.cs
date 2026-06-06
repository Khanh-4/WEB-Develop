using TechSpecs.ViewModels.Builder;

namespace TechSpecs.Services;

public interface ICompatibilityEngine
{
    Task<FilteredResult> FilterAsync(BuildState state);
}
