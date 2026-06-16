// web/ViewModels/HomeViewModel.cs
namespace TechSpecs.ViewModels;

public class HomeViewModel
{
    public List<TechSpecs.Models.PrebuiltPc> PrebuiltPcs { get; set; } = new();

    // Hero spotlight cards — all nullable; omit card when null
    public TechSpecs.Models.FlashSale? SpotlightFlash { get; set; }
    public SpotlightProductDto? SpotlightNew  { get; set; }
    public SpotlightProductDto? SpotlightHot  { get; set; }
}

public record SpotlightProductDto(
    int      Id,
    string   Category,   // "cpu" | "gpu"
    string   Name,
    decimal  Price,
    string?  ImageUrl,
    DateTime CreatedAt
);
