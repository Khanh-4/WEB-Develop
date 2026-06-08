using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;

namespace TechSpecs.Tests.Helpers;

/// <summary>
/// Creates a fresh in-memory AppDbContext seeded with controlled test data.
/// Note: EF.Functions.ILike is Postgres-specific; tests must leave SearchQuery/Brand null
/// to avoid triggering ILike filters in ApplyPass1.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string dbName)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(opts);
    }

    /// <summary>Seeds a minimal but realistic set of hardware components for tests.</summary>
    public static AppDbContext CreateSeeded(string dbName)
    {
        var db = Create(dbName);

        db.Cpus.AddRange(
            new Cpu { Id = 1, Name = "Intel Core i5-13600K", Manufacturer = "Intel",
                Socket = "LGA1700", CoreCount = 14, ThreadCount = 20,
                BaseClock = 3.5m, BoostClock = 5.1m, TDP = 125,
                ApproximatePerformance = 80, Price = 5_000_000, Stock = 100 },
            new Cpu { Id = 2, Name = "AMD Ryzen 5 7600X", Manufacturer = "AMD",
                Socket = "AM5", CoreCount = 6, ThreadCount = 12,
                BaseClock = 4.7m, BoostClock = 5.3m, TDP = 105,
                ApproximatePerformance = 75, Price = 4_500_000, Stock = 100 },
            new Cpu { Id = 3, Name = "Intel Core i3-12100", Manufacturer = "Intel",
                Socket = "LGA1700", CoreCount = 4, ThreadCount = 8,
                BaseClock = 3.3m, BoostClock = 4.3m, TDP = 60,
                ApproximatePerformance = 40, Price = 2_500_000, Stock = 100 }
        );

        db.Motherboards.AddRange(
            new Motherboard { Id = 10, Name = "ASUS Z790 Pro", Manufacturer = "ASUS",
                SocketCompatibility = "LGA1700", FormFactor = "ATX",
                MemoryCompatibility = "DDR5", MemorySlots = 4,
                Price = 6_000_000, Stock = 100 },
            new Motherboard { Id = 11, Name = "MSI B650 Tomahawk", Manufacturer = "MSI",
                SocketCompatibility = "AM5", FormFactor = "ATX",
                MemoryCompatibility = "DDR5", MemorySlots = 4,
                Price = 4_800_000, Stock = 100 },
            new Motherboard { Id = 12, Name = "Gigabyte B760M DS3H", Manufacturer = "Gigabyte",
                SocketCompatibility = "LGA1700", FormFactor = "mATX",
                MemoryCompatibility = "DDR4", MemorySlots = 2,
                Price = 2_800_000, Stock = 100 }
        );

        db.Memories.AddRange(
            new Memory { Id = 20, Name = "Corsair DDR5-6000 32GB", Manufacturer = "Corsair",
                Type = "DDR5", Capacity = 32, Modules = 2, Speed = 6000,
                Price = 3_000_000 },
            new Memory { Id = 21, Name = "Kingston DDR4-3200 16GB", Manufacturer = "Kingston",
                Type = "DDR4", Capacity = 16, Modules = 2, Speed = 3200,
                Price = 1_500_000 }
        );

        db.VideoCards.AddRange(
            new VideoCard { Id = 30, Name = "RTX 4070", Manufacturer = "NVIDIA",
                VRAM = 12, Length = 310, TDP = 200,
                ApproximatePerformance = 450, Price = 14_000_000, Stock = 100 },
            new VideoCard { Id = 31, Name = "RX 6600", Manufacturer = "AMD",
                VRAM = 8, Length = 240, TDP = 132,
                ApproximatePerformance = 250, Price = 6_000_000, Stock = 100 },
            new VideoCard { Id = 32, Name = "RTX 4090", Manufacturer = "NVIDIA",
                VRAM = 24, Length = 340, TDP = 450,
                ApproximatePerformance = 999, Price = 50_000_000, Stock = 100 }
        );

        db.PowerSupplies.AddRange(
            new PowerSupply { Id = 40, Name = "Corsair RM850x", Manufacturer = "Corsair",
                Wattage = 850, Efficiency = "80+ Gold", Modular = "Full",
                Price = 3_500_000 },
            new PowerSupply { Id = 41, Name = "EVGA 550W", Manufacturer = "EVGA",
                Wattage = 550, Efficiency = "80+ Bronze", Modular = "Non-Modular",
                Price = 1_500_000 },
            new PowerSupply { Id = 42, Name = "Seasonic Focus GX-1000", Manufacturer = "Seasonic",
                Wattage = 1000, Efficiency = "80+ Gold", Modular = "Full",
                Price = 5_000_000 }
        );

        db.CaseEnclosures.AddRange(
            new CaseEnclosure { Id = 50, Name = "NZXT H510 Airflow", Manufacturer = "NZXT",
                FormFactorSupport = "ATX,mATX,ITX", MaxVGALength = 360,
                Color = "Black", Price = 2_500_000 },
            new CaseEnclosure { Id = 51, Name = "Fractal Meshify C", Manufacturer = "Fractal",
                FormFactorSupport = "ATX,mATX", MaxVGALength = 315,
                Color = "Black", Price = 2_800_000 },
            new CaseEnclosure { Id = 52, Name = "Cooler Master NR200", Manufacturer = "Cooler Master",
                FormFactorSupport = "ITX", MaxVGALength = 330,
                Color = "Black", Price = 2_000_000 }
        );

        db.CpuCoolers.AddRange(
            new CpuCooler { Id = 60, Name = "Noctua NH-D15", Manufacturer = "Noctua",
                Type = "Air Tower", SocketCompatibility = "LGA1700,LGA1200,AM5,AM4",
                MaxTDP = 250, Height = 165, Price = 2_000_000 },
            new CpuCooler { Id = 61, Name = "NZXT Kraken 240", Manufacturer = "NZXT",
                Type = "AIO 240mm", SocketCompatibility = "LGA1700,AM5,AM4",
                MaxTDP = 280, Height = 52, Price = 3_000_000 },
            new CpuCooler { Id = 62, Name = "ID-Cooling SE-214", Manufacturer = "ID-Cooling",
                Type = "Air Tower", SocketCompatibility = "LGA1700,LGA1200",
                MaxTDP = 130, Height = 154, Price = 500_000 }
        );

        db.Storages.AddRange(
            new Storage { Id = 70, Name = "Samsung 980 Pro 1TB", Manufacturer = "Samsung",
                Type = "NVMe", Capacity = 1000, Interface = "PCIe 4.0",
                ReadSpeed = 7000, WriteSpeed = 5000, Price = 2_500_000 },
            new Storage { Id = 71, Name = "WD Blue 2TB HDD", Manufacturer = "WD",
                Type = "HDD", Capacity = 2000, Interface = "SATA",
                ReadSpeed = 150, WriteSpeed = 150, Price = 1_200_000 }
        );

        db.SaveChanges();
        return db;
    }
}
