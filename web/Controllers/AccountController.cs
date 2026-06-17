using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechSpecs.Data;
using TechSpecs.Models;
using TechSpecs.Services;
using TechSpecs.ViewModels;

namespace TechSpecs.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;
    private readonly AppDbContext _db;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        AppDbContext db,
        ILogger<AccountController> logger)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
            return LocalRedirect(model.ReturnUrl ?? "/");

        if (result.IsLockedOut)
            ModelState.AddModelError(string.Empty, "Account locked. Try again later.");
        else
            ModelState.AddModelError(string.Empty, "Invalid email or password.");

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) return RedirectToAction(nameof(Login));

        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);

        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/");

        // First time with this Google account
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var name  = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;

        // If a local account with same email already exists, link it to Google and sign in
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            await _userManager.AddLoginAsync(existingUser, info);
            await _signInManager.SignInAsync(existingUser, isPersistent: false);
            return LocalRedirect(returnUrl ?? "/");
        }

        // Brand-new user — create account
        var user = new ApplicationUser { UserName = email, Email = email, FullName = name };
        var createResult = await _userManager.CreateAsync(user);

        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(user, info);
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl ?? "/");
        }

        // Fallback: show specific errors on login page
        foreach (var e in createResult.Errors)
            TempData["LoginError"] = e.Description;
        return RedirectToAction(nameof(Login));
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var logins = await _userManager.GetLoginsAsync(user);
        return View(new ProfileViewModel
        {
            FullName  = user.FullName ?? string.Empty,
            Email     = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt,
            IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google"),
            TotalSpend    = user.TotalSpend,
            LoyaltyPoints = user.LoyaltyPoints,
        });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var logins = await _userManager.GetLoginsAsync(user);
        model.Email = user.Email ?? string.Empty;
        model.CreatedAt = user.CreatedAt;
        model.IsGoogleAccount = logins.Any(l => l.LoginProvider == "Google");

        if (!ModelState.IsValid) return View("Profile", model);

        // Update full name and refresh cookie so navbar claim updates immediately
        user.FullName = model.FullName.Trim();
        await _userManager.UpdateAsync(user);
        await _signInManager.RefreshSignInAsync(user);

        // Change password (only for non-Google accounts that provided current password)
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            if (model.IsGoogleAccount)
            {
                ModelState.AddModelError(string.Empty, "Google accounts cannot set a password here.");
                return View("Profile", model);
            }
            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is required.");
                return View("Profile", model);
            }
            var pwResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!pwResult.Succeeded)
            {
                foreach (var e in pwResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View("Profile", model);
            }
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // ── Email Change ──────────────────────────────────────────────────────

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestEmailChange(string newEmail)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider == "Google"))
        {
            TempData["Error"] = "Google accounts cannot change their email here.";
            return RedirectToAction(nameof(Profile));
        }

        if (string.IsNullOrWhiteSpace(newEmail) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(newEmail))
        {
            TempData["Error"] = "Please enter a valid email address.";
            return RedirectToAction(nameof(Profile));
        }

        newEmail = newEmail.Trim().ToLowerInvariant();

        if (newEmail == (user.Email ?? string.Empty).ToLowerInvariant())
        {
            TempData["Error"] = "New email must be different from your current email.";
            return RedirectToAction(nameof(Profile));
        }

        var existing = await _userManager.FindByEmailAsync(newEmail);
        if (existing != null)
        {
            TempData["Error"] = "This email is already in use by another account.";
            return RedirectToAction(nameof(Profile));
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
        var confirmLink = Url.Action(nameof(ConfirmEmailChange), "Account",
            new { userId = user.Id, newEmail, token }, Request.Scheme)!;

        await _emailSender.SendEmailAsync(
            newEmail,
            "Xác nhận thay đổi email — TechSpecs",
            $"""
            <div style="font-family:sans-serif;max-width:480px;margin:auto">
              <h2 style="color:#7c3aed">TechSpecs — Xác nhận email mới</h2>
              <p>Bạn đã yêu cầu thay đổi email tài khoản TechSpecs sang địa chỉ này.</p>
              <p>Click vào nút bên dưới để xác nhận. Link có hiệu lực trong <strong>24 giờ</strong>.</p>
              <a href="{confirmLink}"
                 style="display:inline-block;padding:12px 28px;background:#7c3aed;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;margin:12px 0">
                Xác nhận email mới
              </a>
              <p style="color:#888;font-size:.85rem">Nếu bạn không yêu cầu thay đổi, hãy bỏ qua email này.</p>
            </div>
            """);

        TempData["Success"] = $"Email xác nhận đã được gửi tới {newEmail}. Vui lòng kiểm tra hộp thư để hoàn tất.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmailChange(string userId, string newEmail, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(token))
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var result = await _userManager.ChangeEmailAsync(user, newEmail, token);
        if (!result.Succeeded)
        {
            TempData["Error"] = "Link xác nhận không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction(nameof(Profile));
        }

        await _userManager.SetUserNameAsync(user, newEmail);
        await _signInManager.RefreshSignInAsync(user);

        return View("ConfirmEmailChange", (object)newEmail);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ── Forgot Password ───────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        // Always redirect — never reveal whether email exists
        if (user == null)
            return RedirectToAction(nameof(ForgotPasswordConfirmation));

        // Block Google-only accounts from resetting (they have no password)
        var logins = await _userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider == "Google") && !await _userManager.HasPasswordAsync(user))
            return RedirectToAction(nameof(ForgotPasswordConfirmation));

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Action(nameof(ResetPassword), "Account",
            new { email = model.Email, token }, Request.Scheme)!;

        try
        {
            await _emailSender.SendEmailAsync(
                model.Email,
                "Reset your TechSpecs password",
                $"""
                <div style="font-family:sans-serif;max-width:480px;margin:auto">
                  <h2 style="color:#7c3aed">TechSpecs — Password Reset</h2>
                  <p>Click the button below to reset your password. The link expires in <strong>1 hour</strong>.</p>
                  <a href="{resetLink}"
                     style="display:inline-block;padding:12px 28px;background:#7c3aed;color:#fff;border-radius:8px;text-decoration:none;font-weight:600;margin:12px 0">
                    Reset Password
                  </a>
                  <p style="color:#888;font-size:.85rem">If you didn't request this, you can ignore this email.</p>
                </div>
                """);
        }
        catch (Exception ex)
        {
            // Email delivery failed (Resend misconfig, domain unverified, network…).
            // Don't leak a 500 to the user — log for ops and show the neutral confirmation.
            _logger.LogError(ex, "Failed to send password reset email to {Email}", model.Email);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    // ── Reset Password ────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation() => View();

    // GET /Account/Warranties — user's own warranty records (via order history)
    [HttpGet, Authorize]
    public async Task<IActionResult> Warranties()
    {
        var userId = _userManager.GetUserId(User)!;
        var orderIds = await _db.Orders
            .Where(o => o.UserId == userId)
            .Select(o => o.Id)
            .ToListAsync();

        var records = await _db.WarrantyRecords
            .Where(w => w.OrderId.HasValue && orderIds.Contains(w.OrderId.Value))
            .OrderByDescending(w => w.PurchaseDate)
            .ToListAsync();

        return View(records);
    }
}
