using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class MockProductRepository : IProductRepository
{
    private readonly List<Product> _products;

    public MockProductRepository()
    {
        _products = new List<Product>
        {
            P(1, "Laptop Dell XPS 13 Plus", 35_000_000, 39_900_000,
                "Intel Core i7 thế hệ 13, 16GB RAM, SSD 512GB, màn hình 13.4\" OLED.",
                "Laptop cao cấp với khung nhôm CNC nguyên khối, trackpad cảm ứng haptic, bàn phím cảm ứng độc đáo và màn hình OLED InfinityEdge sắc nét.",
                1, "Laptop", "/images/laptop-dell-1.svg",
                new[] { "/images/laptop-dell-2.svg", "/images/laptop-dell-3.svg" },
                rating: 4.7, reviewCount: 124, sold: 312, isHot: true, isNew: false),

            P(2, "MacBook Air M2 13\"", 28_500_000, 32_000_000,
                "Chip Apple M2 8 lõi, 8GB Unified Memory, SSD 256GB.",
                "MacBook Air M2 thiết kế mỏng nhẹ 1.24kg, hiệu năng vượt trội với chip M2, pin 18 giờ, màn hình Liquid Retina 13.6\".",
                1, "Laptop", "/images/laptop-mac-1.svg",
                new[] { "/images/laptop-mac-2.svg", "/images/laptop-mac-3.svg" },
                rating: 4.9, reviewCount: 287, sold: 540, isHot: true, isNew: true),

            P(3, "Asus TUF Gaming F15", 24_900_000, null,
                "Intel Core i5-12500H, RTX 4060 8GB, 16GB DDR5, SSD 512GB.",
                "Laptop gaming chuẩn quân đội MIL-STD-810H, màn hình 15.6\" 144Hz, hệ thống tản nhiệt 5 ống đồng.",
                1, "Laptop", "/images/laptop-asus-1.svg",
                new[] { "/images/laptop-asus-2.svg" },
                rating: 4.4, reviewCount: 89, sold: 156, isHot: false, isNew: false),

            P(4, "Lenovo ThinkPad X1 Carbon Gen 11", 42_000_000, 46_500_000,
                "Intel Core i7 vPro, 16GB RAM, SSD 1TB, màn 14\" 2.8K OLED.",
                "ThinkPad X1 Carbon — biểu tượng laptop doanh nhân. Chuẩn quân đội, bàn phím trứ danh, bảo mật vân tay + IR.",
                1, "Laptop", "/images/laptop-lenovo-1.svg",
                new[] { "/images/laptop-lenovo-2.svg" },
                rating: 4.8, reviewCount: 76, sold: 98, isHot: false, isNew: true),

            P(5, "PC Gaming Asus ROG Strix G35", 55_000_000, 62_000_000,
                "Intel Core i9-13900K, RTX 4080 16GB, 32GB DDR5, NVMe 1TB.",
                "Build PC gaming top-tier với case ROG độc quyền, RGB Aura Sync, làm mát AIO 360mm — chiến mượt mọi tựa game 4K.",
                2, "Desktop", "/images/desktop-asus-1.svg",
                new[] { "/images/desktop-asus-2.svg" },
                rating: 4.6, reviewCount: 41, sold: 64, isHot: true, isNew: false),

            P(6, "HP Pavilion Desktop TP01", 18_500_000, null,
                "Intel Core i5-12400, 8GB RAM, SSD 256GB + HDD 1TB.",
                "PC văn phòng HP Pavilion hiệu năng ổn định cho công việc, học tập và giải trí cơ bản.",
                2, "Desktop", "/images/desktop-hp-1.svg",
                new[] { "/images/desktop-hp-2.svg" },
                rating: 4.3, reviewCount: 32, sold: 121, isHot: false, isNew: false),

            P(7, "Dell Inspiron All-in-One 24", 22_000_000, 24_500_000,
                "Intel Core i5, 8GB RAM, SSD 512GB, 23.8\" FHD.",
                "All-in-One gọn gàng cho bàn làm việc, webcam pop-up, loa stereo, chân đế xoay nghiêng.",
                2, "Desktop", "/images/desktop-aio-1.svg",
                Array.Empty<string>(),
                rating: 4.2, reviewCount: 28, sold: 47, isHot: false, isNew: true),

            P(8, "Chuột Logitech MX Master 3S", 2_590_000, 2_990_000,
                "Cảm biến 8000 DPI, sạc USB-C, kết nối 3 thiết bị.",
                "Chuột flagship của Logitech với thiết kế công thái học, click siêu êm SilentTouch, scroll MagSpeed.",
                3, "Phụ kiện", "/images/mouse-1.svg",
                new[] { "/images/mouse-2.svg" },
                rating: 4.9, reviewCount: 412, sold: 1280, isHot: true, isNew: false),

            P(9, "Bàn phím Keychron K2 Pro", 2_990_000, null,
                "Layout 75%, switch Gateron hot-swap, RGB.",
                "Bàn phím cơ không dây Keychron K2 Pro, hỗ trợ QMK/VIA, vỏ nhôm tuỳ chọn, kết nối Bluetooth 5.1.",
                3, "Phụ kiện", "/images/keyboard-1.svg",
                new[] { "/images/keyboard-2.svg" },
                rating: 4.7, reviewCount: 198, sold: 530, isHot: false, isNew: true),

            P(10, "Tai nghe Sony WH-1000XM5", 8_490_000, 9_990_000,
                "Chống ồn chủ động hàng đầu, pin 30h, codec LDAC.",
                "Sony WH-1000XM5 — tai nghe chống ồn được nhiều reviewer đánh giá tốt nhất 2024, tích hợp Dual Noise Sensor.",
                3, "Phụ kiện", "/images/headphone-1.svg",
                new[] { "/images/headphone-2.svg" },
                rating: 4.8, reviewCount: 356, sold: 870, isHot: true, isNew: false),

            P(11, "Webcam Logitech C920 HD Pro", 1_890_000, null,
                "Full HD 1080p, micro stereo, auto-focus.",
                "Webcam phổ biến nhất cho streamer/họp online — chất lượng hình ảnh ổn định, plug-and-play.",
                3, "Phụ kiện", "/images/webcam-1.svg",
                Array.Empty<string>(),
                rating: 4.5, reviewCount: 612, sold: 1920, isHot: false, isNew: false),

            P(12, "Màn hình LG UltraGear 27GP850", 9_500_000, 11_200_000,
                "27\" QHD 165Hz, Nano IPS, 1ms, G-Sync compatible.",
                "Màn hình gaming top-seller — màu sắc Nano IPS, refresh 165Hz, hỗ trợ HDR400 và G-Sync.",
                4, "Màn hình", "/images/monitor-1.svg",
                new[] { "/images/monitor-2.svg" },
                rating: 4.6, reviewCount: 145, sold: 380, isHot: true, isNew: true),
        };
    }

    /// <summary>Helper khởi tạo Product gọn.</summary>
    private static Product P(int id, string name, decimal price, decimal? oldPrice,
        string shortDesc, string desc, int catId, string cat,
        string main, IEnumerable<string> extras,
        double rating, int reviewCount, int sold, bool isHot, bool isNew)
        => new()
        {
            Id = id, Name = name, Slug = Slugify(name),
            Price = price, OldPrice = oldPrice,
            ShortDescription = shortDesc, Description = desc,
            CategoryId = catId, Category = cat,
            ImageUrl = main, ImageUrls = extras.ToList(),
            Rating = rating, ReviewCount = reviewCount, Sold = sold,
            IsHot = isHot, IsNew = isNew, Stock = 50 + id * 3,
            CreatedAt = DateTime.UtcNow.AddDays(-id * 3)
        };

    private static string Slugify(string s) =>
        new string(s.ToLowerInvariant()
            .Replace('đ', 'd')
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-')
            .Replace("--", "-");

    public IEnumerable<Product> GetAll() => _products;

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public void Add(Product product)
    {
        product.Id = _products.Count == 0 ? 1 : _products.Max(p => p.Id) + 1;
        product.Slug = Slugify(product.Name);
        product.CreatedAt = DateTime.UtcNow;
        product.IsNew = true;
        _products.Add(product);
    }

    public void Update(Product product)
    {
        var idx = _products.FindIndex(p => p.Id == product.Id);
        if (idx < 0) return;
        product.Slug = Slugify(product.Name);
        _products[idx] = product;
    }

    public void Delete(int id) => _products.RemoveAll(p => p.Id == id);
}
