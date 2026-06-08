using TechSpecs.Models;

namespace TechSpecs.Services;

public class MockDataService : IMockDataService
{
    public List<FlashSaleItem> GetFlashSales()
    {
        return new List<FlashSaleItem>
        {
            new FlashSaleItem
            {
                Id = 1,
                Name = "Intel Core i9-14900K",
                Category = "cpu",
                ImageUrl = "https://placehold.co/300x300/1e1b4b/a855f7?text=i9-14900K",
                OriginalPrice = 16990000,
                SalePrice = 14590000,
                TotalQuantity = 20,
                SoldQuantity = 15,
                EndTime = DateTime.Now.AddHours(5).AddMinutes(30)
            },
            new FlashSaleItem
            {
                Id = 2,
                Name = "ASUS ROG Strix RTX 4090 OC",
                Category = "gpu",
                ImageUrl = "https://placehold.co/300x300/1e1b4b/a855f7?text=RTX+4090",
                OriginalPrice = 59990000,
                SalePrice = 52990000,
                TotalQuantity = 10,
                SoldQuantity = 8,
                EndTime = DateTime.Now.AddHours(2)
            },
            new FlashSaleItem
            {
                Id = 3,
                Name = "Corsair Vengeance RGB 32GB (2x16GB) DDR5 6000MHz",
                Category = "memory",
                ImageUrl = "https://placehold.co/300x300/1e1b4b/a855f7?text=DDR5+32GB",
                OriginalPrice = 3590000,
                SalePrice = 2890000,
                TotalQuantity = 50,
                SoldQuantity = 45,
                EndTime = DateTime.Now.AddHours(12)
            },
            new FlashSaleItem
            {
                Id = 4,
                Name = "Samsung 990 PRO 2TB PCIe 4.0 NVMe",
                Category = "storage",
                ImageUrl = "https://placehold.co/300x300/1e1b4b/a855f7?text=990+PRO+2TB",
                OriginalPrice = 5290000,
                SalePrice = 4390000,
                TotalQuantity = 30,
                SoldQuantity = 12,
                EndTime = DateTime.Now.AddHours(8)
            }
        };
    }

    public List<PrebuiltPcItem> GetPrebuiltPcs()
    {
        return new List<PrebuiltPcItem>
        {
            new PrebuiltPcItem
            {
                Id = 1,
                Name = "PC Gaming ROG Strix G15",
                Purpose = "Gaming",
                ImageUrl = "https://placehold.co/300x300/1a1a1a/FFF?text=PC+Gaming",
                Price = 35990000,
                OldPrice = 39990000,
                CpuBadge = "Core i7-13700F",
                GpuBadge = "RTX 4060 Ti",
                RamBadge = "16GB DDR5",
                StorageBadge = "1TB Gen4"
            },
            new PrebuiltPcItem
            {
                Id = 2,
                Name = "PC Office Pro Mini",
                Purpose = "Office",
                ImageUrl = "https://placehold.co/300x300/1a1a1a/FFF?text=PC+Office",
                Price = 12590000,
                OldPrice = 14500000,
                CpuBadge = "Core i5-12400",
                GpuBadge = "UHD 730",
                RamBadge = "16GB DDR4",
                StorageBadge = "500GB NVMe"
            },
            new PrebuiltPcItem
            {
                Id = 3,
                Name = "PC Creator Workstation Alpha",
                Purpose = "Creator",
                ImageUrl = "https://placehold.co/300x300/1a1a1a/FFF?text=PC+Creator",
                Price = 65990000,
                OldPrice = null,
                CpuBadge = "Ryzen 9 7950X",
                GpuBadge = "RTX 4080 Super",
                RamBadge = "64GB DDR5",
                StorageBadge = "2TB Gen4"
            },
            new PrebuiltPcItem
            {
                Id = 4,
                Name = "PC Gaming Entry Level",
                Purpose = "Gaming",
                ImageUrl = "https://placehold.co/300x300/1a1a1a/FFF?text=PC+Gaming+Entry",
                Price = 18990000,
                OldPrice = 21000000,
                CpuBadge = "Core i5-12400F",
                GpuBadge = "RTX 3060",
                RamBadge = "16GB DDR4",
                StorageBadge = "500GB NVMe"
            }
        };
    }
}
