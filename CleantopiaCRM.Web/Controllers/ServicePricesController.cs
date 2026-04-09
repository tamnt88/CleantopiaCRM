using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class ServicePricesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? category, bool? isActive, decimal? minPrice, decimal? maxPrice, int page = 1, int pageSize = 10)
    {
        var query = db.ServicePrices.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.ServiceName.Contains(q) || (x.VariantName ?? "").Contains(q));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (minPrice.HasValue) query = query.Where(x => x.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(x => x.Price <= maxPrice.Value);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.Category).ThenBy(x => x.DisplayOrder).ThenBy(x => x.ServiceName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Category = category;
        ViewBag.IsActive = isActive;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
        return View(new PagedResult<ServicePrice> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public IActionResult Create() => View(new ServicePrice());

    [HttpPost]
    public async Task<IActionResult> Create(ServicePrice item)
    {
        if (!ModelState.IsValid) return View(item);
        db.ServicePrices.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ServicePrices.FindAsync(id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ServicePrice item)
    {
        var old = await db.ServicePrices.FindAsync(id);
        if (old is null) return NotFound();
        old.ServiceName = item.ServiceName;
        old.Category = item.Category;
        old.VariantName = item.VariantName;
        old.Unit = item.Unit;
        old.Price = item.Price;
        old.Description = item.Description;
        old.IsActive = item.IsActive;
        old.DisplayOrder = item.DisplayOrder;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ServicePrices.FindAsync(id);
        if (item is not null)
        {
            db.ServicePrices.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
