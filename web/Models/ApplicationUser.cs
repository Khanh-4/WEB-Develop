using Microsoft.AspNetCore.Identity;

namespace TechSpecs.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal TotalSpend { get; set; }
    public int LoyaltyPoints { get; set; }
}

public static class MembershipTier
{
    public record Tier(string Name, string NameVi, string Color, string Icon, decimal MinSpend);

    public static readonly Tier[] Tiers =
    [
        new("Diamond", "Kim Cương", "#a78bfa", "bi-gem",         20_000_000m),
        new("Gold",    "Vàng",      "#f59e0b", "bi-star-fill",    5_000_000m),
        new("Silver",  "Bạc",       "#94a3b8", "bi-shield-fill",  1_000_000m),
        new("Bronze",  "Đồng",      "#b45309", "bi-person-fill",          0m),
    ];

    public static Tier Resolve(decimal totalSpend) =>
        Tiers.First(t => totalSpend >= t.MinSpend);

    // 1 point per 10,000đ
    public static int PointsFor(decimal amount) => (int)(amount / 10_000m);

    // Progress to next tier (0-100)
    public static (decimal next, int pct) Progress(decimal totalSpend)
    {
        for (int i = Tiers.Length - 1; i >= 1; i--)
        {
            if (totalSpend < Tiers[i - 1].MinSpend)
            {
                var low  = Tiers[i].MinSpend;
                var high = Tiers[i - 1].MinSpend;
                var pct  = (int)((totalSpend - low) / (high - low) * 100);
                return (high, Math.Clamp(pct, 0, 99));
            }
        }
        return (0, 100); // Diamond — max
    }
}
