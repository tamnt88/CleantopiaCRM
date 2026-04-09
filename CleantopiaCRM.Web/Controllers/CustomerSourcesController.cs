using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class CustomerSourcesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? isActive, int page = 1, int pageSize = 10)
    {
        var query = db.CustomerSources.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Name.Contains(q));
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.IsActive = isActive?.ToString().ToLowerInvariant();

        return View(new PagedResult<CustomerSource> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public IActionResult Create() => View(new CustomerSource { IsActive = true });

    [HttpPost]
    public async Task<IActionResult> Create(CustomerSource item)
    {
        if (!ModelState.IsValid) return View(item);
        db.CustomerSources.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CustomerSources.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CustomerSource item)
    {
        var old = await db.CustomerSources.FindAsync(id);
        if (old is null) return NotFound();

        old.Name = item.Name;
        old.IsActive = item.IsActive;
        old.SortOrder = item.SortOrder;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CustomerSources.FindAsync(id);
        if (item is not null)
        {
            db.CustomerSources.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
