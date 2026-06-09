using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang_Bai2.Models;

namespace WebBanHang_Bai2.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager) => _userManager = userManager;

    public async Task<IActionResult> Index(string? keyword)
    {
        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(u =>
                u.UserName!.ToLower().Contains(kw) ||
                u.Email!.ToLower().Contains(kw) ||
                u.FullName.ToLower().Contains(kw));
        }

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        foreach (var u in users)
            u.Role = (await _userManager.GetRolesAsync(u)).FirstOrDefault() ?? "Customer";

        ViewBag.Keyword = keyword;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is not null && user.UserName != "admin")
            await _userManager.DeleteAsync(user);
        TempData["Success"] = "Đã xoá người dùng.";
        return RedirectToAction(nameof(Index));
    }
}
