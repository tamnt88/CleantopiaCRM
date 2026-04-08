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
        var revenue = await db.QuoteItems
            .Where(i => i.Quote != null && i.Quote.Status == "Approved")
            .SumAsync(i => (decimal?) (i.Quantity * i.UnitPrice)) ?? 0m;

        var vm = new DashboardVm
        {
            CustomerCount = await db.Customers.CountAsync(),
            AppointmentToday = await db.Appointments.CountAsync(x => x.ScheduledAt.Date == today),
            Revenue = revenue,
            PendingReminders = await db.MaintenanceReminders.CountAsync(x => !x.IsDone && x.NextReminderDate <= today.AddDays(7))
        };
        return View(vm);
    }
}
