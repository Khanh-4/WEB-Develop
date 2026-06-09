using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedUsersAsync(userManager);
        await SeedCategoriesAsync(context);
        await SeedProductsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Customer" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        async Task CreateUser(string userName, string email, string fullName, string password, string role,
            string? phone = null, string? address = null)
        {
            if (await userManager.FindByNameAsync(userName) is not null) return;
            var user = new ApplicationUser
            {
                UserName = userName, Email = email, FullName = fullName,
                Phone = phone, Address = address, CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded) await userManager.AddToRoleAsync(user, role);
        }

        await CreateUser("admin", "admin@techstore.local", "Quản trị viên", "admin123", "Admin");
        await CreateUser("khachhang", "kh@techstore.local", "Nguyễn Khách Hàng", "123456", "Customer",
            phone: "0901234567", address: "1 Nguyễn Huệ, Q.1, TP.HCM");
        await CreateUser("minhtuan", "minhtuan@example.com", "Minh Tuấn", "123456", "Customer");
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (await context.Categories.AnyAsync()) return;

        context.Categories.AddRange(
            new Category { Name = "Laptop",    Icon = "bi-laptop",     Description = "Laptop văn phòng, gaming, đồ hoạ" },
            new Category { Name = "Desktop",   Icon = "bi-pc-display", Description = "Máy tính để bàn & All-in-One" },
            new Category { Name = "Phụ kiện",  Icon = "bi-mouse2",     Description = "Chuột, bàn phím, tai nghe, webcam" },
            new Category { Name = "Màn hình",  Icon = "bi-display",    Description = "Màn hình gaming, đồ hoạ, văn phòng" }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync()) return;

        var cats = await context.Categories.ToListAsync();
        int Cat(string name) => cats.First(c => c.Name == name).Id;

        static string Slug(string s) =>
            new string(s.ToLowerInvariant().Replace('đ', 'd')
                .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');

        static Product P(string name, decimal price, decimal? oldPrice, string shortDesc, string desc,
            int catId, string cat, string main, List<string>? extras,
            double rating, int reviewCount, int sold, bool isHot, bool isNew, int stock) => new()
        {
            Name = name, Slug = Slug(name), Price = price, OldPrice = oldPrice,
            ShortDescription = shortDesc, Description = desc,
            CategoryId = catId, Category = cat,
            ImageUrl = main, ImageUrls = extras ?? new List<string>(),
            Rating = rating, ReviewCount = reviewCount, Sold = sold,
            IsHot = isHot, IsNew = isNew, Stock = stock,
            CreatedAt = DateTime.UtcNow.AddDays(-sold / 10)
        };

        context.Products.AddRange(
            P("Laptop Dell XPS 13 Plus", 35_000_000, 39_900_000,
                "Intel Core i7 thế hệ 13, 16GB RAM, SSD 512GB, màn hình 13.4\" OLED.",
                "Laptop cao cấp với khung nhôm CNC nguyên khối, trackpad haptic, bàn phím cảm ứng và màn hình OLED InfinityEdge.",
                Cat("Laptop"), "Laptop", "/images/laptop-dell-1.svg",
                new() { "/images/laptop-dell-2.svg", "/images/laptop-dell-3.svg" },
                4.7, 124, 312, true, false, 53),

            P("MacBook Air M2 13\"", 28_500_000, 32_000_000,
                "Chip Apple M2 8 lõi, 8GB Unified Memory, SSD 256GB.",
                "MacBook Air M2 thiết kế mỏng nhẹ 1.24kg, hiệu năng vượt trội, pin 18 giờ, màn hình Liquid Retina 13.6\".",
                Cat("Laptop"), "Laptop", "/images/laptop-mac-1.svg",
                new() { "/images/laptop-mac-2.svg", "/images/laptop-mac-3.svg" },
                4.9, 287, 540, true, true, 56),

            P("Asus TUF Gaming F15", 24_900_000, null,
                "Intel Core i5-12500H, RTX 4060 8GB, 16GB DDR5, SSD 512GB.",
                "Laptop gaming chuẩn quân đội MIL-STD-810H, màn hình 15.6\" 144Hz, tản nhiệt 5 ống đồng.",
                Cat("Laptop"), "Laptop", "/images/laptop-asus-1.svg",
                new() { "/images/laptop-asus-2.svg" },
                4.4, 89, 156, false, false, 59),

            P("Lenovo ThinkPad X1 Carbon Gen 11", 42_000_000, 46_500_000,
                "Intel Core i7 vPro, 16GB RAM, SSD 1TB, màn 14\" 2.8K OLED.",
                "ThinkPad X1 Carbon — biểu tượng laptop doanh nhân. Chuẩn quân đội, bàn phím trứ danh, bảo mật vân tay + IR.",
                Cat("Laptop"), "Laptop", "/images/laptop-lenovo-1.svg",
                new() { "/images/laptop-lenovo-2.svg" },
                4.8, 76, 98, false, true, 62),

            P("PC Gaming Asus ROG Strix G35", 55_000_000, 62_000_000,
                "Intel Core i9-13900K, RTX 4080 16GB, 32GB DDR5, NVMe 1TB.",
                "Build PC gaming top-tier với case ROG độc quyền, RGB Aura Sync, làm mát AIO 360mm.",
                Cat("Desktop"), "Desktop", "/images/desktop-asus-1.svg",
                new() { "/images/desktop-asus-2.svg" },
                4.6, 41, 64, true, false, 65),

            P("HP Pavilion Desktop TP01", 18_500_000, null,
                "Intel Core i5-12400, 8GB RAM, SSD 256GB + HDD 1TB.",
                "PC văn phòng HP Pavilion hiệu năng ổn định cho công việc, học tập và giải trí cơ bản.",
                Cat("Desktop"), "Desktop", "/images/desktop-hp-1.svg",
                new() { "/images/desktop-hp-2.svg" },
                4.3, 32, 121, false, false, 68),

            P("Dell Inspiron All-in-One 24", 22_000_000, 24_500_000,
                "Intel Core i5, 8GB RAM, SSD 512GB, 23.8\" FHD.",
                "All-in-One gọn gàng cho bàn làm việc, webcam pop-up, loa stereo, chân đế xoay nghiêng.",
                Cat("Desktop"), "Desktop", "/images/desktop-aio-1.svg",
                new(),
                4.2, 28, 47, false, true, 71),

            P("Chuột Logitech MX Master 3S", 2_590_000, 2_990_000,
                "Cảm biến 8000 DPI, sạc USB-C, kết nối 3 thiết bị.",
                "Chuột flagship Logitech, thiết kế công thái học, click siêu êm SilentTouch, scroll MagSpeed.",
                Cat("Phụ kiện"), "Phụ kiện", "/images/mouse-1.svg",
                new() { "/images/mouse-2.svg" },
                4.9, 412, 1280, true, false, 74),

            P("Bàn phím Keychron K2 Pro", 2_990_000, null,
                "Layout 75%, switch Gateron hot-swap, RGB.",
                "Bàn phím cơ không dây Keychron K2 Pro, hỗ trợ QMK/VIA, vỏ nhôm tuỳ chọn, Bluetooth 5.1.",
                Cat("Phụ kiện"), "Phụ kiện", "/images/keyboard-1.svg",
                new() { "/images/keyboard-2.svg" },
                4.7, 198, 530, false, true, 77),

            P("Tai nghe Sony WH-1000XM5", 8_490_000, 9_990_000,
                "Chống ồn chủ động hàng đầu, pin 30h, codec LDAC.",
                "Sony WH-1000XM5 — tai nghe chống ồn tốt nhất 2024, tích hợp Dual Noise Sensor.",
                Cat("Phụ kiện"), "Phụ kiện", "/images/headphone-1.svg",
                new() { "/images/headphone-2.svg" },
                4.8, 356, 870, true, false, 80),

            P("Webcam Logitech C920 HD Pro", 1_890_000, null,
                "Full HD 1080p, micro stereo, auto-focus.",
                "Webcam phổ biến nhất cho streamer/họp online — chất lượng ổn định, plug-and-play.",
                Cat("Phụ kiện"), "Phụ kiện", "/images/webcam-1.svg",
                new(),
                4.5, 612, 1920, false, false, 83),

            P("Màn hình LG UltraGear 27GP850", 9_500_000, 11_200_000,
                "27\" QHD 165Hz, Nano IPS, 1ms, G-Sync compatible.",
                "Màn hình gaming top-seller — màu sắc Nano IPS, refresh 165Hz, HDR400 và G-Sync.",
                Cat("Màn hình"), "Màn hình", "/images/monitor-1.svg",
                new() { "/images/monitor-2.svg" },
                4.6, 145, 380, true, true, 86)
        );

        await context.SaveChangesAsync();
    }
}
