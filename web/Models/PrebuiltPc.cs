using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechSpecs.Models;

[Table("prebuilt_pcs")]
public class PrebuiltPc
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Purpose { get; set; }

    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // FK references to component tables
    public int? CpuId { get; set; }
    public int? MotherboardId { get; set; }
    public int? MemoryId { get; set; }
    public int? VideoCardId { get; set; }
    public int? StorageId { get; set; }
    public int? PowerSupplyId { get; set; }
    public int? CaseId { get; set; }
    public int? CoolerId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CpuId))]
    public Cpu? Cpu { get; set; }

    [ForeignKey(nameof(MotherboardId))]
    public Motherboard? Motherboard { get; set; }

    [ForeignKey(nameof(MemoryId))]
    public Memory? Memory { get; set; }

    [ForeignKey(nameof(VideoCardId))]
    public VideoCard? VideoCard { get; set; }

    [ForeignKey(nameof(StorageId))]
    public Storage? Storage { get; set; }

    [ForeignKey(nameof(PowerSupplyId))]
    public PowerSupply? PowerSupply { get; set; }

    [ForeignKey(nameof(CaseId))]
    public CaseEnclosure? Case { get; set; }

    [ForeignKey(nameof(CoolerId))]
    public CpuCooler? Cooler { get; set; }
}
