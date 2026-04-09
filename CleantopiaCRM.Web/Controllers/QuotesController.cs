using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class QuotesController(AppDbContext db, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(string? q, string? status, int? customerId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
    {
        var query = db.Quotes.Include(x => x.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.QuoteNo.Contains(q) || x.Customer!.Name.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.QuoteDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.QuoteDate <= toDate.Value.Date);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Status = status;
        ViewBag.CustomerId = customerId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        return View(new PagedResult<Quote> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookup();
        return View(new Quote { QuoteDate = DateTime.Today, ValidUntil = DateTime.Today.AddDays(7), Status = "Draft" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Quote quote, int servicePriceId, decimal quantity)
    {
        var service = await db.ServicePrices.FindAsync(servicePriceId);
        if (service is null) return NotFound();

        quote.QuoteNo = $"BG-{DateTime.Now:yyyyMMddHHmmss}";
        quote.Items.Add(new QuoteItem { ServicePriceId = service.Id, Quantity = quantity, UnitPrice = service.Price });
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var quote = await db.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();
        await LoadLookup();
        return View(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Quote item)
    {
        var old = await db.Quotes.FindAsync(id);
        if (old is null) return NotFound();
        old.CustomerId = item.CustomerId;
        old.QuoteDate = item.QuoteDate;
        old.ValidUntil = item.ValidUntil;
        old.Status = item.Status;
        old.Notes = item.Notes;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var quote = await db.Quotes.FindAsync(id);
        if (quote is not null)
        {
            db.Quotes.Remove(quote);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Approve(int id)
    {
        var quote = await db.Quotes.FindAsync(id);
        if (quote is null) return NotFound();
        var wasApproved = string.Equals(quote.Status, "Approved", StringComparison.OrdinalIgnoreCase);
        quote.Status = "Approved";

        if (!wasApproved)
        {
            var items = await db.QuoteItems
                .Include(x => x.ServicePrice)
                    .ThenInclude(x => x!.Policy)
                .Where(x => x.QuoteId == id)
                .ToListAsync();

            foreach (var item in items)
            {
                if (item.ServicePrice is null) continue;

                var cycleDays = item.ServicePrice.Policy?.RecareCycleDays ?? 180;
                if (cycleDays <= 0) cycleDays = 180;

                var serviceName = item.ServicePrice.ServiceName;
                if (!string.IsNullOrWhiteSpace(item.ServicePrice.VariantName))
                    serviceName += $" ({item.ServicePrice.VariantName})";

                db.MaintenanceReminders.Add(new MaintenanceReminder
                {
                    CustomerId = quote.CustomerId,
                    ServiceName = serviceName,
                    CycleDays = cycleDays,
                    LastServiceDate = quote.QuoteDate.Date,
                    NextReminderDate = quote.QuoteDate.Date.AddDays(cycleDays),
                    IsDone = false
                });
            }
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportExcel(int id)
    {
        var quote = await db.Quotes.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.ServicePrice).FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("BaoGia");
        ws.Cell(1, 1).Value = "CONG TY TNHH DICH VU VE SINH CLEAN TOPIA";
        ws.Cell(2, 1).Value = "Dia chi: 77 Ta Hien, Phuong Cat Lai, TP Ho Chi Minh";
        ws.Cell(3, 1).Value = "Hotline: 090 769 1010";
        ws.Cell(5, 1).Value = "BAO GIA DICH VU";
        ws.Cell(6, 1).Value = $"So bao gia: {quote.QuoteNo}";
        ws.Cell(7, 1).Value = $"Khach hang: {quote.Customer?.Name}";
        ws.Cell(8, 1).Value = $"Ngay bao gia: {quote.QuoteDate:dd/MM/yyyy}";
        ws.Cell(10, 1).Value = "Dich vu";
        ws.Cell(10, 2).Value = "So luong";
        ws.Cell(10, 3).Value = "Don gia";
        ws.Cell(10, 4).Value = "Thanh tien";

        var row = 11;
        foreach (var i in quote.Items)
        {
            ws.Cell(row, 1).Value = i.ServicePrice?.ServiceName;
            ws.Cell(row, 2).Value = i.Quantity;
            ws.Cell(row, 3).Value = i.UnitPrice;
            ws.Cell(row, 4).Value = i.Quantity * i.UnitPrice;
            row++;
        }

        ws.Cell(row + 1, 3).Value = "Tong";
        ws.Cell(row + 1, 4).Value = quote.Items.Sum(x => x.Quantity * x.UnitPrice);
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{quote.QuoteNo}.xlsx");
    }

    public async Task<IActionResult> ExportPdf(int id)
    {
        var quote = await db.Quotes.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.ServicePrice).FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        QuestPDF.Settings.License = LicenseType.Community;
        var total = quote.Items.Sum(x => x.Quantity * x.UnitPrice);
        var logoPath = Path.Combine(env.WebRootPath, "images", "logo.png");
        var logoBytes = System.IO.File.Exists(logoPath) ? System.IO.File.ReadAllBytes(logoPath) : null;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Row(row =>
                {
                    row.ConstantItem(80).Height(60).Element(e =>
                    {
                        if (logoBytes is not null) e.Image(logoBytes);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("CONG TY TNHH DICH VU VE SINH CLEAN TOPIA").Bold().FontSize(12);
                        c.Item().Text("77 Ta Hien, Phuong Cat Lai, TP Ho Chi Minh");
                        c.Item().Text("Hotline: 090 769 1010");
                    });
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Item().AlignCenter().Text("BAO GIA DICH VU").Bold().FontSize(16);
                    col.Item().Text($"So bao gia: {quote.QuoteNo}");
                    col.Item().Text($"Khach hang: {quote.Customer?.Name}");
                    col.Item().Text($"Ngay bao gia: {quote.QuoteDate:dd/MM/yyyy}");

                    col.Item().PaddingTop(10).Table(t =>
                    {
                        t.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                        t.Header(h =>
                        {
                            h.Cell().Text("Dich vu").Bold();
                            h.Cell().Text("SL").Bold();
                            h.Cell().Text("Don gia").Bold();
                            h.Cell().Text("Thanh tien").Bold();
                        });

                        foreach (var i in quote.Items)
                        {
                            t.Cell().Text(i.ServicePrice?.ServiceName ?? string.Empty);
                            t.Cell().Text(i.Quantity.ToString("N2"));
                            t.Cell().Text(i.UnitPrice.ToString("N0"));
                            t.Cell().Text((i.Quantity * i.UnitPrice).ToString("N0"));
                        }
                    });

                    col.Item().AlignRight().Text($"Tong cong: {total:N0} VND").Bold().FontSize(12);
                    col.Item().PaddingTop(12).Text("Dieu khoan: Bao gia co hieu luc den het ngay hieu luc. Gia chua bao gom VAT (neu co). Thanh toan theo hop dong.");
                    col.Item().PaddingTop(20).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Text("DAI DIEN KHACH HANG\n(Ky, ghi ro ho ten)");
                        r.RelativeItem().AlignCenter().Text("DAI DIEN CLEAN TOPIA\n(Ky, dong dau)");
                    });
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"{quote.QuoteNo}.pdf");
    }

    private async Task LoadLookup()
    {
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Services = await db.ServicePrices.Where(x => x.IsActive).OrderBy(x => x.ServiceName).ToListAsync();
    }
}
