using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,KyThuat,GiamSat")]
public class AppointmentsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, string? type, int? customerId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
    {
        var query = db.Appointments.Include(x => x.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Customer!.Name.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.ScheduledAt.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.ScheduledAt.Date <= toDate.Value.Date);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.ScheduledAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Status = status;
        ViewBag.Type = type;
        ViewBag.CustomerId = customerId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Types = await db.Appointments.Select(x => x.Type).Distinct().OrderBy(x => x).ToListAsync();
        return View(new PagedResult<Appointment> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(new Appointment { ScheduledAt = DateTime.Now.AddDays(1), Status = "Planned", Type = "Khao sat" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Appointment item)
    {
        db.Appointments.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Appointments.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Appointment item)
    {
        var old = await db.Appointments.FindAsync(id);
        if (old is null) return NotFound();
        old.CustomerId = item.CustomerId;
        old.ScheduledAt = item.ScheduledAt;
        old.Type = item.Type;
        old.Status = item.Status;
        old.Notes = item.Notes;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Appointments.FindAsync(id);
        if (item is not null)
        {
            db.Appointments.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
