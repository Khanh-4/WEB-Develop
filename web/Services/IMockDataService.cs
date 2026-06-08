using TechSpecs.Models;

namespace TechSpecs.Services;

public interface IMockDataService
{
    List<FlashSaleItem> GetFlashSales();
    List<PrebuiltPcItem> GetPrebuiltPcs();
}
