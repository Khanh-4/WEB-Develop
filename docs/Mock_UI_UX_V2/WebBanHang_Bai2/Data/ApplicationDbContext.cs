using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Product: computed property + JSON list
        builder.Entity<Product>()
            .Ignore(p => p.DiscountPercent);

        builder.Entity<Product>()
            .Property(p => p.ImageUrls)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    c => c.Aggregate(0, (h, v) => HashCode.Combine(h, v.GetHashCode())),
                    c => c.ToList()
                )
            )
            .HasColumnType("text");

        // Order: Items as owned entity table (OrderDetails)
        builder.Entity<Order>().OwnsMany(o => o.Items, od =>
        {
            od.WithOwner().HasForeignKey("OrderId");
            od.Property<int>("Id").ValueGeneratedOnAdd();
            od.HasKey("Id");
            od.Ignore(x => x.LineTotal);
            od.ToTable("OrderDetails");
        });

        builder.Entity<Order>()
            .HasIndex(o => o.OrderCode)
            .IsUnique();
    }
}
