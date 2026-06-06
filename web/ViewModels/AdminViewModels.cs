using System.ComponentModel.DataAnnotations;
using TechSpecs.Models;
using TechSpecs.ViewModels;

namespace TechSpecs.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PendingOrders { get; set; }
    public int TotalProducts { get; set; }
    public List<OrderSummaryViewModel> RecentOrders { get; set; } = new();
    public Dictionary<string, int> ProductCountByCategory { get; set; } = new();
}

public class AdminProductEditViewModel
{
    public string Category { get; set; } = string.Empty;
    public int Id { get; set; }  // 0 = new

    // Common fields
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Manufacturer { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int Stock { get; set; }
    public string? ImageUrl { get; set; }

    // CPU / GPU / Cooler shared
    public int? TDP { get; set; }

    // CPU specific
    public string? Socket { get; set; }
    public int? CoreCount { get; set; }
    public int? ThreadCount { get; set; }
    public decimal? BaseClock { get; set; }
    public decimal? BoostClock { get; set; }
    public decimal? CpuPerformance { get; set; }

    // Motherboard specific
    public string? SocketCompatibility { get; set; }
    public string? FormFactor { get; set; }
    public string? MemoryCompatibility { get; set; }
    public int? MemorySlots { get; set; }
    public int? MaxMemoryCapacity { get; set; }

    // Memory specific
    public string? MemoryType { get; set; }
    public int? Capacity { get; set; }
    public int? Modules { get; set; }
    public int? Speed { get; set; }

    // GPU specific
    public int? VRAM { get; set; }
    public int? GpuLength { get; set; }
    public decimal? GpuPerformance { get; set; }

    // PSU specific
    public int? Wattage { get; set; }
    public string? Efficiency { get; set; }
    public string? Modular { get; set; }

    // Case specific
    public string? FormFactorSupport { get; set; }
    public int? MaxVGALength { get; set; }
    public string? Color { get; set; }

    // Storage specific
    public string? StorageType { get; set; }
    public int? StorageCapacity { get; set; }
    public string? Interface { get; set; }
    public int? ReadSpeed { get; set; }
    public int? WriteSpeed { get; set; }

    // Cooler specific
    public string? CoolerSocketCompatibility { get; set; }
    public int? MaxTDP { get; set; }
    public int? Height { get; set; }
    public string? CoolerType { get; set; }
}

public class AdminOrdersViewModel
{
    public List<AdminOrderRowViewModel> Orders { get; set; } = new();
    public string? StatusFilter { get; set; }
}

public class AdminOrderRowViewModel
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
}
