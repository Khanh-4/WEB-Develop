using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebBanHang_Bai2.Models;
using WebBanHang_Bai2.Repositories;

namespace WebBanHang_Bai2.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderRepository _orders;

    public AccountController(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOrderRepository orders)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _orders = orders;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.UserName, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(vm);
        }

        var user = await _userManager.FindByNameAsync(vm.UserName);
        TempData["Success"] = $"Xin chào, {user?.FullName ?? vm.UserName}!";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return await _userManager.IsInRoleAsync(user!, "Admin")
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            UserName = vm.UserName,
            Email = vm.Email,
            FullName = vm.FullName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError(string.Empty, err.Description);
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        TempData["Success"] = "Đăng ký thành công. Vui lòng đăng nhập.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Đã đăng xuất.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.FindByNameAsync(User.Identity?.Name ?? "");
        if (user is null) return RedirectToAction(nameof(Login));
        user.Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Customer";
        return View(user);
    }

    [Authorize]
    public IActionResult Orders()
    {
        var name = User.Identity?.Name ?? "";
        return View(_orders.GetByUserName(name).ToList());
    }
}
