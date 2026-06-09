using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Data;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Repositories;

public class EFOrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public EFOrderRepository(ApplicationDbContext context) => _context = context;

    public IEnumerable<Order> GetAll() =>
        _context.Orders.Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToList();

    public Order? GetById(int id) =>
        _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.Id == id);

    public Order? GetByCode(string code) =>
        _context.Orders.Include(o => o.Items).FirstOrDefault(o => o.OrderCode == code);

    public IEnumerable<Order> GetByUserName(string userName) =>
        _context.Orders.Include(o => o.Items)
            .Where(o => o.UserName == userName)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

    public void Add(Order order)
    {
        if (string.IsNullOrEmpty(order.OrderCode))
            order.OrderCode = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
        order.CreatedAt = DateTime.UtcNow;
        _context.Orders.Add(order);
        _context.SaveChanges();
    }

    public void UpdateStatus(int id, OrderStatus status)
    {
        var o = _context.Orders.Find(id);
        if (o is not null)
        {
            o.Status = status;
            _context.SaveChanges();
        }
    }

    public void Delete(int id)
    {
        var o = _context.Orders.Find(id);
        if (o is not null)
        {
            _context.Orders.Remove(o);
            _context.SaveChanges();
        }
    }
}
