using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class RemindersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
    {
        var query = db.MaintenanceReminders.Include(x => x.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Customer!.Name.Contains(q) || x.ServiceName.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.NextReminderDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new PagedResult<MaintenanceReminder> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(new MaintenanceReminder { LastServiceDate = DateTime.Today, NextReminderDate = DateTime.Today.AddDays(90), CycleDays = 90 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(MaintenanceReminder item)
    {
        item.NextReminderDate = item.LastServiceDate.AddDays(item.CycleDays);
        db.MaintenanceReminders.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.MaintenanceReminders.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, MaintenanceReminder item)
    {
        var old = await db.MaintenanceReminders.FindAsync(id);
        if (old is null) return NotFound();
        old.CustomerId = item.CustomerId;
        old.ServiceName = item.ServiceName;
        old.CycleDays = item.CycleDays;
        old.LastServiceDate = item.LastServiceDate;
        old.NextReminderDate = item.LastServiceDate.AddDays(item.CycleDays);
        old.IsDone = item.IsDone;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.MaintenanceReminders.FindAsync(id);
        if (item is not null)
        {
            db.MaintenanceReminders.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MarkDone(int id)
    {
        var reminder = await db.MaintenanceReminders.FindAsync(id);
        if (reminder is null) return NotFound();
        reminder.LastServiceDate = DateTime.Today;
        reminder.NextReminderDate = DateTime.Today.AddDays(reminder.CycleDays);
        reminder.IsDone = false;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
