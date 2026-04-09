using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class FeedbacksController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, int? customerId, int? rating, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
    {
        var query = db.ServiceFeedbacks.Include(x => x.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.Customer!.Name.Contains(q) || (x.Content ?? "").Contains(q));
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (rating.HasValue) query = query.Where(x => x.Rating == rating.Value);
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt.Date <= toDate.Value.Date);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.CustomerId = customerId;
        ViewBag.Rating = rating;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(new PagedResult<ServiceFeedback> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(new ServiceFeedback { CreatedAt = DateTime.Now, Rating = 5 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceFeedback item)
    {
        db.ServiceFeedbacks.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ServiceFeedbacks.FindAsync(id);
        if (item is null) return NotFound();
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ServiceFeedback item)
    {
        var old = await db.ServiceFeedbacks.FindAsync(id);
        if (old is null) return NotFound();
        old.CustomerId = item.CustomerId;
        old.Rating = item.Rating;
        old.Content = item.Content;
        old.CreatedAt = item.CreatedAt;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ServiceFeedbacks.FindAsync(id);
        if (item is not null)
        {
            db.ServiceFeedbacks.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
