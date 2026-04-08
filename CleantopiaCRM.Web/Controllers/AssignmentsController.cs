using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class AssignmentsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 10)
    {
        var query = db.Assignments.Include(x => x.Appointment).ThenInclude(a => a!.Customer).Include(x => x.Employee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Employee!.FullName.Contains(q) || x.Appointment!.Customer!.Name.Contains(q));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return View(new PagedResult<Assignment> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Appointments = await db.Appointments.Include(x => x.Customer).OrderByDescending(x => x.ScheduledAt).ToListAsync();
        ViewBag.Employees = await db.Employees.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        return View(new Assignment());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Assignment item)
    {
        db.Assignments.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Assignments.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Appointments = await db.Appointments.Include(x => x.Customer).OrderByDescending(x => x.ScheduledAt).ToListAsync();
        ViewBag.Employees = await db.Employees.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Assignment item)
    {
        var old = await db.Assignments.FindAsync(id);
        if (old is null) return NotFound();
        old.AppointmentId = item.AppointmentId;
        old.EmployeeId = item.EmployeeId;
        old.Role = item.Role;
        old.SupervisionNote = item.SupervisionNote;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Assignments.FindAsync(id);
        if (item is not null)
        {
            db.Assignments.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
