using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechSpecs.Controllers;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Tests.Helpers;
using Xunit;

namespace TechSpecs.Tests;

public class CartTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly CartController _controller;
    private const string TestUserId = "user-test-001";

    public CartTests()
    {
        _db = DbContextFactory.Create($"cart_{Guid.NewGuid()}");

        var userManager = MockUserManager(TestUserId);
        _controller = new CartController(_db, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public void Dispose() => _db.Dispose();

    // ── Add ──────────────────────────────────────────────────────

    [Fact]
    public async Task Add_NewItem_ReturnsCountOne()
    {
        var result = await _controller.Add(new AddToCartRequest("cpu", 1, "Intel i5", 5_000_000, null));

        var ok = Assert.IsType<OkObjectResult>(result);
        var data = ok.Value!;
        Assert.Equal(1, (int)data.GetType().GetProperty("count")!.GetValue(data)!);
    }

    [Fact]
    public async Task Add_SameItemTwice_IncrementsQuantity()
    {
        var req = new AddToCartRequest("cpu", 1, "Intel i5", 5_000_000, null);
        await _controller.Add(req);
        var result = await _controller.Add(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var count = (int)ok.Value!.GetType().GetProperty("count")!.GetValue(ok.Value)!;
        Assert.Equal(2, count);

        // Only one CartItem row — quantity should be 2
        var item = _db.CartItems.Single();
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task Add_DifferentItems_CreatesMultipleRows()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU A", 5_000_000, null));
        await _controller.Add(new AddToCartRequest("gpu", 2, "GPU B", 10_000_000, null));

        Assert.Equal(2, _db.CartItems.Count());
    }

    [Fact]
    public async Task Add_InvalidRequest_ReturnsBadRequest()
    {
        var result = await _controller.Add(new AddToCartRequest("", 0, "", 0, null));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // ── Remove ───────────────────────────────────────────────────

    [Fact]
    public async Task Remove_ExistingItem_DecreasesCount()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));
        await _controller.Add(new AddToCartRequest("gpu", 2, "GPU", 10_000_000, null));

        var itemId = _db.CartItems.First(i => i.Category == "cpu").Id;
        var result = await _controller.Remove(new RemoveFromCartRequest(itemId));

        var ok = Assert.IsType<OkObjectResult>(result);
        var count = (int)ok.Value!.GetType().GetProperty("count")!.GetValue(ok.Value)!;
        Assert.Equal(1, count);
        Assert.Equal(1, _db.CartItems.Count());
    }

    [Fact]
    public async Task Remove_NonExistentItem_ReturnsNotFound()
    {
        var result = await _controller.Remove(new RemoveFromCartRequest(999));
        Assert.IsType<NotFoundResult>(result);
    }

    // ── Update Qty ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateQty_ValidQuantity_UpdatesItem()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));
        var itemId = _db.CartItems.First().Id;

        var result = await _controller.UpdateQty(new UpdateQtyRequest(itemId, 3));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(3, _db.CartItems.Find(itemId)!.Quantity);
    }

    [Fact]
    public async Task UpdateQty_ZeroQuantity_ReturnsBadRequest()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));
        var itemId = _db.CartItems.First().Id;

        var result = await _controller.UpdateQty(new UpdateQtyRequest(itemId, 0));
        Assert.IsType<BadRequestResult>(result);
    }

    // ── Clear ────────────────────────────────────────────────────

    [Fact]
    public async Task Clear_RemovesAllItemsFromCart()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));
        await _controller.Add(new AddToCartRequest("gpu", 2, "GPU", 10_000_000, null));

        await _controller.Clear();

        Assert.Equal(0, _db.CartItems.Count());
    }

    // ── Count ────────────────────────────────────────────────────

    [Fact]
    public async Task Count_ReturnsZeroForEmptyCart()
    {
        var result = await _controller.Count();
        var ok = Assert.IsType<OkObjectResult>(result);
        var count = (int)ok.Value!.GetType().GetProperty("count")!.GetValue(ok.Value)!;
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Count_ReflectsActualItemTotal()
    {
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null)); // qty → 2
        await _controller.Add(new AddToCartRequest("gpu", 2, "GPU", 10_000_000, null)); // + 1

        var result = await _controller.Count();
        var ok = Assert.IsType<OkObjectResult>(result);
        var count = (int)ok.Value!.GetType().GetProperty("count")!.GetValue(ok.Value)!;
        Assert.Equal(3, count);
    }

    // ── Cart isolation ───────────────────────────────────────────

    [Fact]
    public async Task Cart_IsIsolatedPerUser()
    {
        // Add item as user A
        await _controller.Add(new AddToCartRequest("cpu", 1, "CPU", 5_000_000, null));

        // Switch controller to user B
        var userB = MockUserManager("user-test-002");
        var controllerB = new CartController(_db, userB)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controllerB.Count();
        var ok = Assert.IsType<OkObjectResult>(result);
        var count = (int)ok.Value!.GetType().GetProperty("count")!.GetValue(ok.Value)!;
        Assert.Equal(0, count);
    }

    // ── helpers ──────────────────────────────────────────────────

    private static UserManager<ApplicationUser> MockUserManager(string userId)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
           .Returns(userId);
        return mgr.Object;
    }
}
