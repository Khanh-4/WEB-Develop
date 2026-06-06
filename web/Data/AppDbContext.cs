using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Models;

namespace TechSpecs.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cpu> Cpus { get; set; }
    public DbSet<Motherboard> Motherboards { get; set; }
    public DbSet<Memory> Memories { get; set; }
    public DbSet<VideoCard> VideoCards { get; set; }
    public DbSet<PowerSupply> PowerSupplies { get; set; }
    public DbSet<CaseEnclosure> CaseEnclosures { get; set; }
    public DbSet<Storage> Storages { get; set; }
    public DbSet<CpuCooler> CpuCoolers { get; set; }

    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<SavedBuild> SavedBuilds { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
