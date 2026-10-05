using System.Security.Claims;
using CurrencyMini.Data;
using CurrencyMini.Models;
using CurrencyMini.Services;
using CurrencyMini.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CurrencyMini.Controllers;

[AllowAnonymous] // login/register must be reachable while logged out (everything else requires login by default)
public class AccountController : Controller
{
    // Verified when the email is unknown, so "no such user" and "wrong password" take the same time.
    private static readonly string DummyHash = PasswordService.Hash("not-a-real-password");

    private readonly AppDbContext _db;

    public AccountController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Currency");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Currency");
        if (!ModelState.IsValid) return View(model);

        var email = NormalizeEmail(model.Email);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var passwordOk = PasswordService.Verify(model.Password, user?.PasswordHash ?? DummyHash);

        if (user is null || !passwordOk)
        {
            // Same message for both cases so the form doesn't reveal which emails are registered.
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        await SignInAsync(user, model.RememberMe);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Currency");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Currency");
        if (!ModelState.IsValid) return View(model);

        var email = NormalizeEmail(model.Email);
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new AppUser
        {
            Email = email,
            PasswordHash = PasswordService.Hash(model.Password),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two people registering the same email at the same instant: the unique index caught it.
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        await SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Currency");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => RedirectToAction(nameof(Login));

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private Task SignInAsync(AppUser user, bool isPersistent)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email)
        }, CookieAuthenticationDefaults.AuthenticationScheme);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = isPersistent });
    }

    // Only follow return URLs that stay on this site (prevents open-redirect attacks).
    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Currency");
}
