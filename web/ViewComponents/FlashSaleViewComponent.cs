using Microsoft.AspNetCore.Mvc;
using TechSpecs.Services;

namespace TechSpecs.ViewComponents;

public class FlashSaleViewComponent : ViewComponent
{
    private readonly IMockDataService _mockData;

    public FlashSaleViewComponent(IMockDataService mockData) => _mockData = mockData;

    public IViewComponentResult Invoke()
    {
        var items = _mockData.GetFlashSales();
        return items.Any() ? View(items) : Content(string.Empty);
    }
}
