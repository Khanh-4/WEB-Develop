using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public interface IOrderRepository
{
    IEnumerable<Order> GetAll();
    Order? GetById(int id);
    Order? GetByCode(string code);
    IEnumerable<Order> GetByUserName(string userName);
    void Add(Order order);
    void UpdateStatus(int id, OrderStatus status);
    void Delete(int id);
}

public class MockOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders;

    public MockOrderRepository()
    {
        _orders = SeedOrders();
    }

    private static List<Order> SeedOrders()
    {
        var rnd = new Random(42);
        var statuses = new[] { OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Shipping, OrderStatus.Completed };
        var names = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường", "Phạm Quỳnh Dung", "Hoàng Minh Em" };
        var emails = new[] { "an@example.com", "binh@example.com", "cuong@example.com", "dung@example.com", "em@example.com" };

        var list = new List<Order>();
        for (var i = 0; i < 18; i++)
        {
            var n = i % names.Length;
            var sub = 1_000_000m * (rnd.Next(2, 60));
            var ship = sub >= 500_000 ? 0 : 30_000;
            list.Add(new Order
            {
                Id = i + 1,
                OrderCode = $"ORD{(20260100 + i)}",
                CustomerName = names[n],
                Email = emails[n],
                Phone = $"09{rnd.Next(10000000, 99999999)}",
                ShippingAddress = $"{rnd.Next(1, 999)} Nguyễn Huệ, Q.1, TP.HCM",
                Notes = null,
                PaymentMethod = i % 3 == 0 ? "VNPay" : "COD",
                Subtotal = sub,
                ShippingFee = ship,
                Total = sub + ship,
                Status = statuses[i % statuses.Length],
                CreatedAt = DateTime.UtcNow.AddDays(-i * 1.3),
                Items = new List<OrderDetail>()
            });
        }
        return list;
    }

    public IEnumerable<Order> GetAll() => _orders.OrderByDescending(o => o.CreatedAt);
    public Order? GetById(int id) => _orders.FirstOrDefault(o => o.Id == id);
    public Order? GetByCode(string code) => _orders.FirstOrDefault(o => o.OrderCode == code);
    public IEnumerable<Order> GetByUserName(string userName) =>
        _orders.Where(o => o.UserName == userName).OrderByDescending(o => o.CreatedAt);

    public void Add(Order order)
    {
        order.Id = _orders.Count == 0 ? 1 : _orders.Max(o => o.Id) + 1;
        if (string.IsNullOrEmpty(order.OrderCode))
            order.OrderCode = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{order.Id:D3}";
        order.CreatedAt = DateTime.UtcNow;
        _orders.Add(order);
    }

    public void UpdateStatus(int id, OrderStatus status)
    {
        var o = GetById(id);
        if (o is not null) o.Status = status;
    }

    public void Delete(int id) => _orders.RemoveAll(o => o.Id == id);
}
