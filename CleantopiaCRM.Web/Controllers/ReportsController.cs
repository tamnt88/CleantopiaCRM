using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class ReportsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddMonths(-1);
        var to = toDate ?? DateTime.Today;

        var data = await db.Quotes
            .Where(x => x.Status == "Approved" && x.QuoteDate >= from && x.QuoteDate <= to)
            .Select(x => new
            {
                Date = x.QuoteDate.Date,
                Total = x.Items.Sum(i => i.Quantity * i.UnitPrice)
            })
            .ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.TotalRevenue = data.Sum(x => x.Total);
        ViewBag.ByDay = data.GroupBy(x => x.Date).Select(g => new Tuple<DateTime, decimal>(g.Key, g.Sum(x => x.Total))).OrderByDescending(x => x.Item1).ToList();
        return View();
    }

    public async Task<IActionResult> Summary(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? new DateTime(DateTime.Today.Year, 1, 1);
        var to = toDate ?? DateTime.Today;

        var approvedQuotes = await db.Quotes
            .Where(x => x.Status == "Approved" && x.QuoteDate >= from && x.QuoteDate <= to)
            .Include(x => x.Items)
            .ToListAsync();

        var byMonth = approvedQuotes
            .GroupBy(x => new { x.QuoteDate.Year, x.QuoteDate.Month })
            .Select(g => new
            {
                Month = $"{g.Key.Month:00}/{g.Key.Year}",
                Revenue = g.Sum(q => q.Items.Sum(i => i.Quantity * i.UnitPrice))
            })
            .OrderBy(x => x.Month)
            .ToList();

        var byService = approvedQuotes
            .SelectMany(q => q.Items)
            .GroupBy(i => i.ServicePriceId)
            .Select(g => new
            {
                ServiceId = g.Key,
                Revenue = g.Sum(i => i.Quantity * i.UnitPrice),
                Quantity = g.Sum(i => i.Quantity)
            })
            .ToList();

        var serviceMap = await db.ServicePrices.ToDictionaryAsync(x => x.Id, x => $"{x.ServiceName} {(string.IsNullOrWhiteSpace(x.VariantName) ? "" : $"({x.VariantName})")}");
        var byServiceVm = byService
            .Select(x => new Tuple<string, decimal, decimal>(serviceMap.GetValueOrDefault(x.ServiceId, "N/A"), x.Quantity, x.Revenue))
            .OrderByDescending(x => x.Item3)
            .ToList();

        var byEmployee = await db.Assignments
            .Where(x => x.Appointment != null && x.Appointment.ScheduledAt >= from && x.Appointment.ScheduledAt <= to)
            .Include(x => x.Employee)
            .GroupBy(x => x.Employee!.FullName)
            .Select(g => new Tuple<string, int>(g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.ByMonth = byMonth;
        ViewBag.ByEmployee = byEmployee;
        ViewBag.ByService = byServiceVm;
        ViewBag.TotalRevenue = approvedQuotes.Sum(q => q.Items.Sum(i => i.Quantity * i.UnitPrice));
        return View();
    }
}
