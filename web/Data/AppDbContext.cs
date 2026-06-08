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
    public DbSet<BuildUpvote> BuildUpvotes { get; set; }
    public DbSet<PriceHistory> PriceHistories { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }
    public DbSet<ProductQuestion> ProductQuestions { get; set; }
    public DbSet<ProductAnswer> ProductAnswers { get; set; }
    public DbSet<WarrantyRecord> WarrantyRecords { get; set; }
    public DbSet<FlashSale> FlashSales { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Bundle> Bundles { get; set; }
    public DbSet<BundleItem> BundleItems { get; set; }
    public DbSet<ComponentBenchmark> ComponentBenchmarks { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BuildUpvote>()
            .HasIndex(u => new { u.BuildId, u.UserId })
            .IsUnique();

        builder.Entity<PriceHistory>()
            .HasIndex(p => new { p.Category, p.ProductName, p.RecordedAt });

        builder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.Category, w.ComponentId })
            .IsUnique();

        builder.Entity<ProductReview>()
            .HasIndex(r => new { r.Category, r.ComponentId });

        builder.Entity<ProductQuestion>()
            .HasIndex(q => new { q.Category, q.ComponentId });

        builder.Entity<WarrantyRecord>()
            .HasIndex(w => w.Phone);

        builder.Entity<WarrantyRecord>()
            .HasIndex(w => w.SerialNumber);
    }
}
