using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class CustomerTypesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, bool? isActive, int page = 1, int pageSize = 10)
    {
        var query = db.CustomerTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Name.Contains(q));
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.IsActive = isActive?.ToString().ToLowerInvariant();

        return View(new PagedResult<CustomerType> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public IActionResult Create() => View(new CustomerType { IsActive = true });

    [HttpPost]
    public async Task<IActionResult> Create(CustomerType item)
    {
        if (!ModelState.IsValid) return View(item);
        db.CustomerTypes.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CustomerTypes.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CustomerType item)
    {
        var old = await db.CustomerTypes.FindAsync(id);
        if (old is null) return NotFound();

        old.Name = item.Name;
        old.IsBusiness = item.IsBusiness;
        old.IsActive = item.IsActive;
        old.SortOrder = item.SortOrder;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CustomerTypes.FindAsync(id);
        if (item is not null)
        {
            db.CustomerTypes.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
