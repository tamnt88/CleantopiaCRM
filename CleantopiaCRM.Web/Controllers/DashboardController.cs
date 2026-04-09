using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

public class DashboardController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var revenue = await db.QuoteItems
            .Where(i => i.Quote != null && i.Quote.Status == "Approved")
            .SumAsync(i => (decimal?)(i.Quantity * i.UnitPrice)) ?? 0m;

        var customerCount = await db.Customers.CountAsync();
        var newCustomersThisMonth = await db.Customers.CountAsync(x => x.CreatedAt >= monthStart);

        var approvedQuotes = await db.Quotes
            .Where(x => x.Status == "Approved")
            .Select(x => new { x.CustomerId, x.QuoteDate })
            .ToListAsync();

        var firstPurchaseCustomersThisMonth = approvedQuotes
            .GroupBy(x => x.CustomerId)
            .Select(g => g.Min(x => x.QuoteDate.Date))
            .Count(d => d >= monthStart);

        var returningCustomersThisMonth = approvedQuotes
            .GroupBy(x => x.CustomerId)
            .Select(g => new { First = g.Min(x => x.QuoteDate.Date), HasThisMonth = g.Any(x => x.QuoteDate.Date >= monthStart) })
            .Count(x => x.First < monthStart && x.HasThisMonth);

        var sourceSummary = await db.Customers
            .Include(x => x.CustomerSource)
            .GroupBy(x => x.CustomerSource != null ? x.CustomerSource.Name : "Khác")
            .Select(g => new SourceSummaryVm
            {
                Source = string.IsNullOrWhiteSpace(g.Key) ? "Khác" : g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var topServices = await db.QuoteItems
            .Where(x => x.Quote != null && x.Quote.Status == "Approved")
            .GroupBy(x => new { x.ServicePriceId, x.ServicePrice!.ServiceName, x.ServicePrice.VariantName })
            .Select(g => new ServiceUsageVm
            {
                ServiceName = g.Key.ServiceName + (string.IsNullOrWhiteSpace(g.Key.VariantName) ? "" : $" ({g.Key.VariantName})"),
                Quantity = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(10)
            .ToListAsync();

        var boughtAny = approvedQuotes.Select(x => x.CustomerId).Distinct().Count();

        var vm = new DashboardVm
        {
            CustomerCount = customerCount,
            AppointmentToday = await db.Appointments.CountAsync(x => x.ScheduledAt.Date == today),
            Revenue = revenue,
            PendingReminders = await db.MaintenanceReminders.CountAsync(x => !x.IsDone && x.NextReminderDate <= today.AddDays(7)),
            NewCustomersThisMonth = newCustomersThisMonth,
            FirstPurchaseCustomersThisMonth = firstPurchaseCustomersThisMonth,
            ReturningCustomersThisMonth = returningCustomersThisMonth,
            FirstPurchaseRate = customerCount == 0 ? 0 : Math.Round((decimal)firstPurchaseCustomersThisMonth * 100 / customerCount, 1),
            ReturningRate = boughtAny == 0 ? 0 : Math.Round((decimal)returningCustomersThisMonth * 100 / boughtAny, 1),
            CustomerBySource = sourceSummary,
            TopServices = topServices
        };

        return View(vm);
    }
}
