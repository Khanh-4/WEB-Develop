using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechSpecs.Controllers;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Tests.Helpers;
using TechSpecs.ViewModels;
using Xunit;

namespace TechSpecs.Tests;

public class OrderTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly OrdersController _orders;
    private readonly CartController _cart;
    private const string TestUserId = "user-order-001";

    public OrderTests()
    {
        _db = DbContextFactory.Create($"order_{Guid.NewGuid()}");
        var userManager = MockUserManager(TestUserId);

        _cart = new CartController(_db, userManager)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _orders = new OrdersController(_db, userManager)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    public void Dispose() => _db.Dispose();

    // ── Checkout GET ─────────────────────────────────────────────

    [Fact]
    public async Task Checkout_Get_WithEmptyCart_RedirectsToCart()
    {
        var result = await _orders.Checkout();
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Cart", redirect.ControllerName);
    }

    [Fact]
    public async Task Checkout_Get_WithItems_ReturnsView()
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);

        var result = await _orders.Checkout();
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<CheckoutViewModel>(view.Model);
        Assert.Single(vm.Cart.Items);
    }

    // ── Checkout POST ────────────────────────────────────────────

    [Fact]
    public async Task Checkout_Post_CreatesOrderWithCorrectItems()
    {
        await AddItemToCart("cpu", 1, "Intel i5", 5_000_000);
        await AddItemToCart("gpu", 2, "RTX 4070", 14_000_000);

        var result = await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Nguyen Van Test",
            Phone = "0912345678",
            ShippingAddress = "123 Test St, HCM"
        });

        var order = _db.Orders.First();
        Assert.Equal(2, order.Details.Count);
        Assert.Equal("Nguyen Van Test", order.RecipientName);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public async Task Checkout_Post_TotalAmountMatchesCartSum()
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);
        await AddItemToCart("gpu", 2, "GPU", 14_000_000);

        await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Test", Phone = "0900000000", ShippingAddress = "Test"
        });

        var order = _db.Orders.First();
        Assert.Equal(19_000_000, order.TotalAmount);
    }

    [Fact]
    public async Task Checkout_Post_ClearsCartAfterSuccess()
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);
        Assert.Equal(1, _db.CartItems.Count());

        await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Test", Phone = "0900000000", ShippingAddress = "Test"
        });

        Assert.Equal(0, _db.CartItems.Count());
    }

    [Fact]
    public async Task Checkout_Post_RedirectsToConfirmation()
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);

        var result = await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Test", Phone = "0900000000", ShippingAddress = "Test"
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Confirmation", redirect.ActionName);
        Assert.NotNull(redirect.RouteValues?["id"]);
    }

    [Fact]
    public async Task Checkout_Post_InvalidModel_ReturnsView()
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);
        _orders.ModelState.AddModelError("Phone", "Required");

        var result = await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Test", Phone = "", ShippingAddress = "Test"
        });

        Assert.IsType<ViewResult>(result);
        Assert.Equal(0, _db.Orders.Count());
    }

    [Fact]
    public async Task Checkout_Post_QuantityGreaterThan1_IsPreservedInOrder()
    {
        // Add same item twice → qty=2
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);

        await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Test", Phone = "0900000000", ShippingAddress = "Test"
        });

        var detail = _db.OrderDetails.First();
        Assert.Equal(2, detail.Quantity);
        Assert.Equal(10_000_000, _db.Orders.First().TotalAmount);
    }

    // ── Orders history ───────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsOnlyCurrentUserOrders()
    {
        await PlaceOrder("Order A");

        // Place order as different user — shouldn't appear in results
        var otherUser = MockUserManager("user-other");
        var otherCart = new CartController(_db, otherUser)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var otherOrders = new OrdersController(_db, otherUser)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        await otherCart.Add(new AddToCartRequest("gpu", 2, "GPU", 10_000_000, null));
        await otherOrders.Checkout(new CheckoutViewModel
        {
            RecipientName = "Other", Phone = "0900000001", ShippingAddress = "Other"
        });

        var result = await _orders.Index();
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<OrderListViewModel>(view.Model);
        Assert.Single(vm.Orders);
    }

    // ── Order detail ─────────────────────────────────────────────

    [Fact]
    public async Task Detail_ReturnsCorrectOrder()
    {
        var orderId = await PlaceOrder("Test");

        var result = await _orders.Detail(orderId);
        var view = Assert.IsType<ViewResult>(result);
        var vm = Assert.IsType<OrderDetailViewModel>(view.Model);
        Assert.Equal(orderId, vm.Id);
        Assert.Equal("0900000000", vm.Phone);
    }

    [Fact]
    public async Task Detail_OtherUsersOrder_ReturnsNotFound()
    {
        var orderId = await PlaceOrder("Test");

        var otherUser = MockUserManager("user-other");
        var otherOrders = new OrdersController(_db, otherUser)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await otherOrders.Detail(orderId);
        Assert.IsType<NotFoundResult>(result);
    }

    // ── helpers ──────────────────────────────────────────────────

    private async Task AddItemToCart(string cat, int id, string name, decimal price)
        => await _cart.Add(new AddToCartRequest(cat, id, name, price, null));

    private async Task<int> PlaceOrder(string name)
    {
        await AddItemToCart("cpu", 1, "CPU", 5_000_000);
        await _orders.Checkout(new CheckoutViewModel
        {
            RecipientName = name, Phone = "0900000000", ShippingAddress = "Test"
        });
        return _db.Orders.OrderByDescending(o => o.Id).First().Id;
    }

    private static UserManager<ApplicationUser> MockUserManager(string userId)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
           .Returns(userId);
        return mgr.Object;
    }
}
