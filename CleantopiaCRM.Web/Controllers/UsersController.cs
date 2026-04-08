using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using CleantopiaCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
    {
        var query = db.AppUsers.Include(x => x.Employee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Username.Contains(q) || x.FullName.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Username).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new PagedResult<AppUser> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Employees = await db.Employees.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        return View(new AppUser { Role = "KyThuat", IsActive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppUser item, string password)
    {
        item.PasswordHash = PasswordHasher.Hash(password);
        db.AppUsers.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.AppUsers.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Employees = await db.Employees.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AppUser item)
    {
        var old = await db.AppUsers.FindAsync(id);
        if (old is null) return NotFound();
        old.FullName = item.FullName;
        old.Role = item.Role;
        old.EmployeeId = item.EmployeeId;
        old.IsActive = item.IsActive;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var user = await db.AppUsers.FindAsync(id);
        if (user is not null)
        {
            user.PasswordHash = PasswordHasher.Hash(newPassword);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.AppUsers.FindAsync(id);
        if (user is not null)
        {
            db.AppUsers.Remove(user);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
