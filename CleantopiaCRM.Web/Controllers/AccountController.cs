using System.Security.Claims;
using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Services;
using CleantopiaCRM.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[AllowAnonymous]
public class AccountController(AppDbContext db) : Controller
{
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var hash = PasswordHasher.Hash(vm.Password);
        Entities.AppUser? user;

        try
        {
            user = await db.AppUsers.FirstOrDefaultAsync(x => x.Username == vm.Username && x.PasswordHash == hash && x.IsActive);
        }
        catch (SqlException)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối cơ sở dữ liệu. Vui lòng thử lại sau.");
            return View(vm);
        }
        catch (InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, "Hệ thống tạm thời gián đoạn kết nối dữ liệu. Vui lòng thử lại.");
            return View(vm);
        }

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
