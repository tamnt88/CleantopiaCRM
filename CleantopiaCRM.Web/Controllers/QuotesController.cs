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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class QuotesController(AppDbContext db, IWebHostEnvironment env) : Controller
{
    private bool IsModalRequest()
    {
        if (string.Equals(Request.Query["modal"], "1", StringComparison.OrdinalIgnoreCase))
            return true;

        return Request.HasFormContentType
               && string.Equals(Request.Form["modal"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ModalSuccessResult()
    {
        if (!IsModalRequest())
            return RedirectToAction(nameof(Index));

        return Content(
            "<script>window.parent && window.parent.CRM && window.parent.CRM.closeQuoteModal(true);</script>",
            "text/html");
    }

    public async Task<IActionResult> Index(string? q, string? status, int? customerId, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
    {
        var query = db.Quotes
            .Include(x => x.Customer)
            .Include(x => x.Items)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.QuoteNo.Contains(q) || x.Customer!.Name.Contains(q));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (fromDate.HasValue) query = query.Where(x => x.QuoteDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.QuoteDate <= toDate.Value.Date);
        var items = await query.OrderByDescending(x => x.Id).ToListAsync();
        var total = items.Count;
        ViewBag.Status = status;
        ViewBag.CustomerId = customerId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        ViewBag.ProvinceMap = await db.GhnProvinces.ToDictionaryAsync(x => x.Id, x => x.ProvinceName);
        ViewBag.WardMap = await db.GhnWards.ToDictionaryAsync(x => x.Id, x => x.WardName);
        return View(new PagedResult<Quote> { Items = items, Page = 1, PageSize = total == 0 ? 20 : total, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookup();
        ViewBag.IsModal = IsModalRequest();
        return View(new Quote { QuoteDate = DateTime.Today, ValidUntil = DateTime.Today.AddDays(7), Status = "Draft" });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Quote quote, int[]? itemServicePriceId, decimal[]? itemQuantity, decimal[]? itemUnitPrice, decimal[]? itemDiscountAmount, string[]? itemNote)
    {
        itemServicePriceId ??= Array.Empty<int>();
        itemQuantity ??= Array.Empty<decimal>();
        itemUnitPrice ??= Array.Empty<decimal>();
        itemDiscountAmount ??= Array.Empty<decimal>();
        itemNote ??= Array.Empty<string>();

        if (quote.CustomerId <= 0)
            ModelState.AddModelError(nameof(quote.CustomerId), "Vui lòng chọn khách hàng.");
        if (string.IsNullOrWhiteSpace(quote.ServiceAddressText))
            ModelState.AddModelError(nameof(quote.ServiceAddressText), "Vui lòng nhập hoặc chọn địa chỉ dịch vụ.");
        if (quote.DiscountAmount < 0)
            ModelState.AddModelError(nameof(quote.DiscountAmount), "Giảm giá chung không hợp lệ.");
        if (quote.VatRate < 0 || quote.VatRate > 100)
            ModelState.AddModelError(nameof(quote.VatRate), "VAT phải trong khoảng 0-100%.");
        if (quote.HasVatInvoice)
        {
            if (string.IsNullOrWhiteSpace(quote.InvoiceCompanyName))
                ModelState.AddModelError(nameof(quote.InvoiceCompanyName), "Vui lòng nhập tên công ty.");
            if (string.IsNullOrWhiteSpace(quote.InvoiceTaxCode))
                ModelState.AddModelError(nameof(quote.InvoiceTaxCode), "Vui lòng nhập mã số thuế.");
            if (string.IsNullOrWhiteSpace(quote.InvoiceAddress))
                ModelState.AddModelError(nameof(quote.InvoiceAddress), "Vui lòng nhập địa chỉ xuất hóa đơn.");
            if (string.IsNullOrWhiteSpace(quote.InvoiceEmail))
                ModelState.AddModelError(nameof(quote.InvoiceEmail), "Vui lòng nhập email nhận hóa đơn.");
            if (string.IsNullOrWhiteSpace(quote.InvoiceReceiver))
                ModelState.AddModelError(nameof(quote.InvoiceReceiver), "Vui lòng nhập người nhận hóa đơn.");
        }

        ValidateQuoteItems(itemServicePriceId, itemQuantity, itemUnitPrice, itemDiscountAmount);
        if (!ModelState.IsValid)
        {
            await LoadLookup();
            ViewBag.IsModal = IsModalRequest();
            return View(quote);
        }

        quote.QuoteNo = $"BG-{DateTime.Now:yyyyMMddHHmmss}";
        quote.Status = string.IsNullOrWhiteSpace(quote.Status) ? "Draft" : quote.Status;
        quote.ServiceAddressText = quote.ServiceAddressText?.Trim();
        quote.ContactName = quote.ContactName?.Trim();
        quote.ContactPhone = quote.ContactPhone?.Trim();
        quote.InvoiceCompanyName = quote.InvoiceCompanyName?.Trim();
        quote.InvoiceTaxCode = quote.InvoiceTaxCode?.Trim();
        quote.InvoiceAddress = quote.InvoiceAddress?.Trim();
        quote.InvoiceEmail = quote.InvoiceEmail?.Trim();
        quote.InvoiceReceiver = quote.InvoiceReceiver?.Trim();
        quote.Items = BuildQuoteItems(itemServicePriceId, itemQuantity, itemUnitPrice, itemDiscountAmount, itemNote);
        ApplyQuoteTotals(quote);
        await SaveQuoteAddressAndInvoiceForReuseAsync(quote);
        db.Quotes.Add(quote);
        await db.SaveChangesAsync();
        return ModalSuccessResult();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var quote = await db.Quotes
            .Include(x => x.Items)
                .ThenInclude(x => x.ServicePrice)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();
        await LoadLookup();
        ViewBag.IsModal = IsModalRequest();
        return View(quote);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Quote item, int[]? itemServicePriceId, decimal[]? itemQuantity, decimal[]? itemUnitPrice, decimal[]? itemDiscountAmount, string[]? itemNote)
    {
        itemServicePriceId ??= Array.Empty<int>();
        itemQuantity ??= Array.Empty<decimal>();
        itemUnitPrice ??= Array.Empty<decimal>();
        itemDiscountAmount ??= Array.Empty<decimal>();
        itemNote ??= Array.Empty<string>();

        var old = await db.Quotes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();

        if (item.CustomerId <= 0)
            ModelState.AddModelError(nameof(item.CustomerId), "Vui lòng chọn khách hàng.");
        if (string.IsNullOrWhiteSpace(item.ServiceAddressText))
            ModelState.AddModelError(nameof(item.ServiceAddressText), "Vui lòng nhập hoặc chọn địa chỉ dịch vụ.");
        if (item.DiscountAmount < 0)
            ModelState.AddModelError(nameof(item.DiscountAmount), "Giảm giá chung không hợp lệ.");
        if (item.VatRate < 0 || item.VatRate > 100)
            ModelState.AddModelError(nameof(item.VatRate), "VAT phải trong khoảng 0-100%.");
        if (item.HasVatInvoice)
        {
            if (string.IsNullOrWhiteSpace(item.InvoiceCompanyName))
                ModelState.AddModelError(nameof(item.InvoiceCompanyName), "Vui lòng nhập tên công ty.");
            if (string.IsNullOrWhiteSpace(item.InvoiceTaxCode))
                ModelState.AddModelError(nameof(item.InvoiceTaxCode), "Vui lòng nhập mã số thuế.");
            if (string.IsNullOrWhiteSpace(item.InvoiceAddress))
                ModelState.AddModelError(nameof(item.InvoiceAddress), "Vui lòng nhập địa chỉ xuất hóa đơn.");
            if (string.IsNullOrWhiteSpace(item.InvoiceEmail))
                ModelState.AddModelError(nameof(item.InvoiceEmail), "Vui lòng nhập email nhận hóa đơn.");
            if (string.IsNullOrWhiteSpace(item.InvoiceReceiver))
                ModelState.AddModelError(nameof(item.InvoiceReceiver), "Vui lòng nhập người nhận hóa đơn.");
        }

        ValidateQuoteItems(itemServicePriceId, itemQuantity, itemUnitPrice, itemDiscountAmount);
        if (!ModelState.IsValid)
        {
            await LoadLookup();
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        old.CustomerId = item.CustomerId;
        old.QuoteDate = item.QuoteDate;
        old.ValidUntil = item.ValidUntil;
        old.Status = item.Status;
        old.ServiceAddressId = item.ServiceAddressId;
        old.ServiceProvinceId = item.ServiceProvinceId;
        old.ServiceWardId = item.ServiceWardId;
        old.ServiceAddressText = item.ServiceAddressText?.Trim();
        old.ContactName = item.ContactName?.Trim();
        old.ContactPhone = item.ContactPhone?.Trim();
        old.HasVatInvoice = item.HasVatInvoice;
        old.InvoiceCompanyName = item.InvoiceCompanyName?.Trim();
        old.InvoiceTaxCode = item.InvoiceTaxCode?.Trim();
        old.InvoiceAddress = item.InvoiceAddress?.Trim();
        old.InvoiceEmail = item.InvoiceEmail?.Trim();
        old.InvoiceReceiver = item.InvoiceReceiver?.Trim();
        old.DiscountAmount = item.DiscountAmount;
        old.VatRate = item.VatRate;
        old.Notes = item.Notes;

        db.QuoteItems.RemoveRange(old.Items);
        old.Items = BuildQuoteItems(itemServicePriceId, itemQuantity, itemUnitPrice, itemDiscountAmount, itemNote);
        ApplyQuoteTotals(old);
        await SaveQuoteAddressAndInvoiceForReuseAsync(old);
        await db.SaveChangesAsync();
        return ModalSuccessResult();
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var quote = await db.Quotes
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is not null)
        {
            if (quote.Items.Count > 0)
                db.QuoteItems.RemoveRange(quote.Items);

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
        var quote = await db.Quotes
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(x => x.ServicePrice)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        var lineTotal = quote.Items.Sum(x => x.Amount);
        var quoteDiscount = quote.DiscountAmount < 0 ? 0 : quote.DiscountAmount;
        var subtotal = Math.Max(0, lineTotal - quoteDiscount);
        var vat = Math.Round(subtotal * (quote.VatRate / 100m), 0, MidpointRounding.AwayFromZero);
        var totalPayable = subtotal + vat;
        var clientCode = string.IsNullOrWhiteSpace(quote.Customer?.CustomerCode)
            ? $"KH-{quote.CustomerId:00000}"
            : quote.Customer.CustomerCode!;
        var phoneText = !string.IsNullOrWhiteSpace(quote.ContactPhone) ? quote.ContactPhone : (quote.Customer?.Phone ?? string.Empty);
        var customerName = !string.IsNullOrWhiteSpace(quote.ContactName) ? quote.ContactName : (quote.Customer?.Name ?? string.Empty);
        var addressText = await BuildFullServiceAddressAsync(quote);
        var vatRequestText = quote.HasVatInvoice ? "Có" : "Không";
        var orgText = quote.HasVatInvoice
            ? (!string.IsNullOrWhiteSpace(quote.InvoiceCompanyName) ? quote.InvoiceCompanyName! : (quote.Customer?.CompanyName ?? customerName))
            : (quote.Customer?.CompanyName ?? customerName);
        var taxText = quote.HasVatInvoice ? (quote.InvoiceTaxCode ?? string.Empty) : (quote.Customer?.TaxCode ?? string.Empty);
        var emailText = quote.HasVatInvoice ? (quote.InvoiceEmail ?? string.Empty) : (quote.Customer?.Email ?? string.Empty);
        var validUntilText = quote.ValidUntil?.ToString("dd/MM/yyyy") ?? string.Empty;
        var invoiceAddressText = quote.HasVatInvoice ? (quote.InvoiceAddress ?? string.Empty) : string.Empty;

        var rootPath = Directory.GetParent(env.ContentRootPath)?.FullName ?? env.ContentRootPath;
        byte[]? LoadImage(params string[] candidates)
        {
            foreach (var relative in candidates)
            {
                var p = Path.Combine(rootPath, relative);
                if (System.IO.File.Exists(p))
                    return System.IO.File.ReadAllBytes(p);
            }
            return null;
        }
        var logoBytes = LoadImage(Path.Combine("document", "logo.png"), Path.Combine("CleantopiaCRM.Web", "wwwroot", "images", "logo.png"));
        var zaloQrBytes = LoadImage(Path.Combine("document", "zalo.jpg"));
        var bankQrBytes = LoadImage(Path.Combine("document", "bank.jpg"));

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("BaoGia");
        ws.Style.Font.FontName = "Times New Roman";
        ws.Style.Font.FontSize = 10;

        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 1);
        ws.PageSetup.Margins.Top = 0.3;
        ws.PageSetup.Margins.Bottom = 0.3;
        ws.PageSetup.Margins.Left = 0.3;
        ws.PageSetup.Margins.Right = 0.3;

        ws.Column(1).Width = 11;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 13;
        ws.Column(4).Width = 16;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 13;
        ws.Column(7).Width = 13;
        ws.Column(8).Width = 12;
        ws.Row(1).Height = 22;
        ws.Row(2).Height = 22;
        ws.Row(3).Height = 22;
        ws.Row(4).Height = 22;
        ws.Row(5).Height = 22;
        ws.Row(6).Height = 22;
        ws.Row(7).Height = 22;
        ws.Row(8).Height = 26;
        ws.Row(9).Height = 26;
        ws.Row(10).Height = 26;

        // Header block
        ws.Range("A1:A4").Merge();
        ws.Range("B1:C4").Merge();
        ws.Range("D1:G1").Merge();
        ws.Range("D2:G2").Merge();
        ws.Range("D3:G3").Merge();
        ws.Range("D4:G4").Merge();
        ws.Range("H1:H4").Merge();
        ws.Range("A1:H4").Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range("A1:H4").Style.Alignment.WrapText = true;

        ws.Cell("B1").Value = string.Empty;
        var companyText = ws.Cell("B1").GetRichText();
        companyText.ClearText();
        companyText.AddText("CÔNG TY TNHH DỊCH VỤ VỆ SINH CLEAN TOPIA").SetBold();
        companyText.AddNewLine();
        companyText.AddText("Địa chỉ: 77 Tạ Hiện, Phường Cát Lái, TP.HCM, Việt Nam");
        companyText.AddNewLine();
        companyText.AddText("MST: 0318114168   Hotline: 090.769.1010");
        companyText.AddNewLine();
        companyText.AddText("Email: info@cleantopia.vn   Website: https://cleantopia.vn/");
        ws.Cell("B1").Style.Alignment.WrapText = true;
        ws.Cell("B1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Cell("B1").Style.Font.FontSize = 9;

        ws.Cell("D1").Value = "ĐƠN ĐĂNG KÝ SỬ DỤNG DỊCH VỤ";
        ws.Cell("D1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("D1").Style.Font.Bold = true;
        ws.Cell("D1").Style.Font.FontSize = 14;
        ws.Cell("D2").Value = "SERVICE APPLICATION FORM";
        ws.Cell("D2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("D2").Style.Font.Bold = true;
        ws.Cell("D2").Style.Font.FontSize = 12;
        ws.Cell("D3").Value = $"PLHĐ số/Order No: {quote.QuoteNo}   Ngày/Date: {quote.QuoteDate:dd/MM/yyyy}";
        ws.Cell("D3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("D4").Value = $"Mã KH/Client code: {clientCode}";
        ws.Cell("D4").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell("H1").Value = "Hotline Zalo";
        ws.Cell("H1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (logoBytes is not null)
        {
            using var logoStream = new MemoryStream(logoBytes);
            var logoPic = ws.AddPicture(logoStream).MoveTo(ws.Cell("A1"));
            logoPic.Width = 84;
            logoPic.Height = 84;
        }
        if (zaloQrBytes is not null)
        {
            using var zaloStream = new MemoryStream(zaloQrBytes);
            var zaloPic = ws.AddPicture(zaloStream).MoveTo(ws.Cell("H2"));
            zaloPic.Width = 78;
            zaloPic.Height = 78;
        }

        ws.Range("A5:F7").Merge().Value = $"Ông/Bà (Đại diện)/Mr.Ms: {customerName}    SDT/Phone no.: {phoneText}\nĐịa chỉ/Address: {addressText}\nYêu cầu xuất hóa đơn/VAT invoice request: {vatRequestText}\nĐịa chỉ xuất hóa đơn/Invoice address: {invoiceAddressText}";
        ws.Range("G5:H7").Merge().Value = $"Ngày đặt lịch/Reservation date: {quote.QuoteDate:dd/MM/yyyy}\nHết hạn báo giá/Valid until: {validUntilText}\nGiờ/Time: {DateTime.Now:hh:mm tt}";
        ws.Range("A5:H7").Style.Alignment.WrapText = true;
        ws.Range("A5:H7").Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

        ws.Range("A8:B10").Merge().Value = "Hình thức và thông tin thanh toán\n/Payment method and Information";
        ws.Range("C8:D10").Merge().Value = "☐ Tiền mặt/Cash\n☐ Chuyển khoản/Bank Transfer";
        ws.Range("E8:G10").Merge().Value = $"Số tài khoản/Bank Acc no.: 003 1811 4168\nNgân hàng/Bank name:\nNgân hàng TMCP Tiên Phong - TP Bank CN Quận 04\nMST/Tax code: {taxText}\nEmail: {emailText}\nCông ty/Organization: {orgText}";
        ws.Range("H8:H10").Merge().Value = "QR Thanh toán\n/QR code";
        ws.Range("A8:H10").Style.Alignment.WrapText = true;
        ws.Range("A8:H10").Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range("A8:H10").Style.Font.FontSize = 9;
        if (bankQrBytes is not null)
        {
            using var bankStream = new MemoryStream(bankQrBytes);
            var bankPic = ws.AddPicture(bankStream).MoveTo(ws.Cell("H9"));
            bankPic.Width = 48;
            bankPic.Height = 48;
        }

        var headerRange = ws.Range("A5:H10");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.Range("A12:H12").Merge().Value = "BẢNG BÁO GIÁ DỊCH VỤ (VND) / QUOTATION (VND)";
        ws.Range("A12:H12").Style.Font.Bold = true;
        ws.Range("A12:H12").Style.Font.FontSize = 9;

        ws.Cell(13, 1).Value = "No.";
        ws.Cell(13, 2).Value = "Hạng mục/Category";
        ws.Cell(13, 3).Value = "Số lượng máy/Units";
        ws.Cell(13, 4).Value = "Số lần/năm/Frequency";
        ws.Cell(13, 5).Value = "Đơn giá/Unit price";
        ws.Cell(13, 6).Value = "Giảm giá/Discount";
        ws.Cell(13, 7).Value = "Thành tiền/Sum";
        ws.Cell(13, 8).Value = "Ghi chú/Note";
        ws.Range("A13:H13").Style.Font.Bold = true;
        ws.Range("A13:H13").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Range("A13:H13").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range("A13:H13").Style.Fill.BackgroundColor = XLColor.FromHtml("#EDEFF5");
        ws.Range("A13:H13").Style.Font.FontSize = 8;
        ws.Row(13).Height = 34;

        var row = 14;
        var no = 1;
        foreach (var i in quote.Items)
        {
            ws.Cell(row, 1).Value = no++;
            ws.Cell(row, 2).Value = i.ServicePrice?.ServiceName;
            ws.Cell(row, 3).Value = i.Quantity;
            ws.Cell(row, 4).Value = 1;
            ws.Cell(row, 5).Value = i.UnitPrice;
            ws.Cell(row, 6).Value = i.DiscountAmount;
            ws.Cell(row, 7).Value = i.Amount;
            ws.Cell(row, 8).Value = i.Note ?? "";
            ws.Range(row, 1, row, 8).Style.Font.FontSize = 8;
            row++;
        }

        ws.Range(row, 1, row, 5).Merge();
        ws.Range(row, 6, row, 7).Merge().Value = "Tổng cộng/Total Amount";
        ws.Cell(row, 8).Value = lineTotal;
        ws.Range(row, 6, row, 8).Style.Font.Bold = true;
        row++;

        if (quoteDiscount > 0)
        {
            ws.Range(row, 1, row, 5).Merge();
            ws.Range(row, 6, row, 7).Merge().Value = "Giảm giá chung/Quote discount";
            ws.Cell(row, 8).Value = quoteDiscount;
            ws.Range(row, 6, row, 8).Style.Font.Bold = true;
            row++;
        }

        ws.Range(row, 1, row, 5).Merge();
        ws.Range(row, 6, row, 7).Merge().Value = $"Thuế VAT ({quote.VatRate:0.##}%)";
        ws.Cell(row, 8).Value = vat;
        ws.Range(row, 6, row, 8).Style.Font.Bold = true;
        row++;

        ws.Range(row, 1, row, 5).Merge();
        ws.Range(row, 6, row, 7).Merge().Value = "Tổng giá trị thanh toán/Total Amount Payable";
        ws.Cell(row, 8).Value = totalPayable;
        ws.Range(row, 6, row, 8).Style.Font.Bold = true;

        var tableEndRow = row;
        ws.Range(13, 1, tableEndRow, 8).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(13, 1, tableEndRow, 8).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        ws.Range(14, 5, tableEndRow, 8).Style.NumberFormat.Format = "#,##0";
        ws.Range(13, 1, tableEndRow, 8).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Range(13, 1, tableEndRow, 8).Style.Alignment.WrapText = true;

        ws.Range($"A{tableEndRow + 2}:H{tableEndRow + 2}").Merge().Value =
            "1. Đơn giá không bao gồm các chi phí khắc phục sự cố hư hỏng và thay thế linh kiện, chi phí thi công khác (Báo giá theo tình hình thực tế). /Unit price excludes repair or replacement costs of components.";
        ws.Range($"A{tableEndRow + 3}:H{tableEndRow + 3}").Merge().Value =
            "2. Thời gian thực hiện dịch vụ kiểm tra và bảo trì tối đa 07 (bảy) ngày làm việc kể từ ngày nhận được yêu cầu. /The service shall be completed within 07 working days upon receipt of request.";
        ws.Range($"A{tableEndRow + 2}:H{tableEndRow + 3}").Style.Alignment.WrapText = true;
        ws.Range($"A{tableEndRow + 2}:H{tableEndRow + 3}").Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        ws.Range($"A{tableEndRow + 2}:H{tableEndRow + 3}").Style.Font.FontSize = 8;
        ws.Row(tableEndRow + 2).Height = 26;
        ws.Row(tableEndRow + 3).Height = 26;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{quote.QuoteNo}.xlsx");
    }

    public async Task<IActionResult> ExportPdf(int id)
    {
        var quote = await db.Quotes
            .Include(x => x.Customer)
                .ThenInclude(c => c!.ServiceAddresses)
                    .ThenInclude(sa => sa.Address)
                    .ThenInclude(a => a!.Ward)
                    .ThenInclude(w => w!.Province)
            .Include(x => x.Items)
                .ThenInclude(x => x.ServicePrice)
                    .ThenInclude(sp => sp!.Policy)
                        .ThenInclude(p => p!.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        QuestPDF.Settings.License = LicenseType.Community;
        var lineTotal = quote.Items.Sum(x => x.Amount);
        var quoteDiscount = quote.DiscountAmount < 0 ? 0 : quote.DiscountAmount;
        var subtotal = Math.Max(0, lineTotal - quoteDiscount);
        var vatRate = quote.VatRate / 100m;
        var vatAmount = Math.Round(subtotal * vatRate, 0, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        var rootPath = Directory.GetParent(env.ContentRootPath)?.FullName ?? env.ContentRootPath;
        byte[]? LoadImage(params string[] candidates)
        {
            foreach (var relative in candidates)
            {
                var p = Path.Combine(rootPath, relative);
                if (System.IO.File.Exists(p))
                    return System.IO.File.ReadAllBytes(p);
            }
            return null;
        }

        var logoBytes = LoadImage(Path.Combine("document", "logo.png"), Path.Combine("CleantopiaCRM.Web", "wwwroot", "images", "logo.png"));
        var zaloQrBytes = LoadImage(Path.Combine("document", "zalo.jpg"));
        var bankQrBytes = LoadImage(Path.Combine("document", "bank.jpg"));

        var addressText = await BuildFullServiceAddressAsync(quote);
        var phoneText = !string.IsNullOrWhiteSpace(quote.ContactPhone) ? quote.ContactPhone : (quote.Customer?.Phone ?? string.Empty);
        var customerName = !string.IsNullOrWhiteSpace(quote.ContactName) ? quote.ContactName : (quote.Customer?.Name ?? string.Empty);
        var vatRequestText = quote.HasVatInvoice ? "Có" : "Không";
        var orgText = quote.HasVatInvoice
            ? (!string.IsNullOrWhiteSpace(quote.InvoiceCompanyName) ? quote.InvoiceCompanyName! : (quote.Customer?.CompanyName ?? customerName))
            : (quote.Customer?.CompanyName ?? customerName);
        var taxText = quote.HasVatInvoice ? (quote.InvoiceTaxCode ?? string.Empty) : (quote.Customer?.TaxCode ?? string.Empty);
        var emailText = quote.HasVatInvoice ? (quote.InvoiceEmail ?? string.Empty) : (quote.Customer?.Email ?? string.Empty);
        var clientCode = string.IsNullOrWhiteSpace(quote.Customer?.CustomerCode)
            ? $"KH-{quote.CustomerId:00000}"
            : quote.Customer.CustomerCode!;
        var invoiceAddressText = quote.HasVatInvoice ? (quote.InvoiceAddress ?? string.Empty) : string.Empty;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(90);
                            cd.RelativeColumn(2.2f);
                            cd.RelativeColumn(3.6f);
                            cd.ConstantColumn(95);
                        });

                        tbl.Cell().Border(0).Padding(4).Height(74).Element(e =>
                        {
                            if (logoBytes is not null) e.AlignTop().AlignCenter().Image(logoBytes).FitArea();
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text("CÔNG TY TNHH DỊCH VỤ VỆ SINH CLEAN TOPIA").Bold().FontSize(9);
                            c.Item().Text("Địa chỉ: 77 Tạ Hiện, Phường Cát Lái, TP.HCM, Việt Nam").FontSize(8);
                            c.Item().Text("MST: 0318114168   Hotline: 090.769.1010").FontSize(8);
                            c.Item().Text("Email: info@cleantopia.vn    Website: https://cleantopia.vn/").FontSize(8);
                        });
                        tbl.Cell().Border(0).Padding(4).AlignTop().Column(c =>
                        {
                            c.Item().AlignCenter().Text("ĐƠN ĐĂNG KÝ SỬ DỤNG DỊCH VỤ").Bold().FontSize(12);
                            c.Item().AlignCenter().Text("SERVICE APPLICATION FORM").Bold().FontSize(11);
                            c.Item().AlignCenter().Text($"PLHĐ số/Order No: {quote.QuoteNo}   Ngày/Date: {quote.QuoteDate:dd/MM/yyyy}").Italic().FontSize(8);
                            c.Item().AlignCenter().Text($"Mã KH/Client code: {clientCode}").Italic().FontSize(8);
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Hotline Zalo").FontSize(8);
                            c.Item().PaddingTop(2).Height(58).Element(e =>
                            {
                                if (zaloQrBytes is not null) e.AlignTop().AlignCenter().Image(zaloQrBytes).FitArea();
                            });
                        });
                    });

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(1);
                        });

                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text($"Công Ty/ Organizations                 : {orgText}");
                            c.Item().Text($"Ông/Bà (Đại diện) Mr/Ms (Represented by) : {customerName}");
                            c.Item().Text($"Địa chỉ/ Address                       : {addressText}");
                            c.Item().Text($"Yêu cầu xuất hóa đơn/ VAT invoice request : {vatRequestText}");
                            c.Item().Text($"Địa chỉ xuất hóa đơn/ Invoice address    : {invoiceAddressText}");
                            c.Item().Text($"Ngày đặt lịch hẹn/ Reservation date    : {quote.QuoteDate:dd/MM/yyyy}");
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text($"MST/ Tax Code: {taxText}");
                            c.Item().Text("Chức vụ/ Position: ");
                            c.Item().Text($"SDT/ Phone no.: {phoneText}");
                            c.Item().Text($"Email: {emailText}");
                            c.Item().Text($"Thông tin liên lạc/ Contact person: {quote.ContactName ?? customerName}");
                            c.Item().Text($"Thời gian/ Time: {DateTime.Now:hh:mm tt}");
                        });
                    });

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2.4f);
                            cd.RelativeColumn(3.2f);
                            cd.RelativeColumn(3.0f);
                            cd.ConstantColumn(120);
                        });

                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().Text("Hình thức và thông tin thanh toán").Bold().FontSize(8);
                            c.Item().Text("/Payment method and Information").FontSize(7);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().PaddingTop(2).Text("☐ Tiền mặt/Cash").FontSize(10);
                            c.Item().Text("☐ Chuyển khoản/Bank Transfer").FontSize(10);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().Text("Số tài khoản/Bank Acc no.: 003 1811 4168").Bold().FontSize(8);
                            c.Item().Text("Ngân hàng/Bank name:").FontSize(8);
                            c.Item().Text("Ngân hàng TMCP Tiên Phong - TP Bank CN Quận 04").Bold().FontSize(8);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Table(qr =>
                        {
                            qr.ColumnsDefinition(qcd =>
                            {
                                qcd.RelativeColumn(1.5f);
                                qcd.RelativeColumn(1f);
                            });

                            qr.Cell().AlignTop().Column(c =>
                            {
                                c.Item().Text("QR Thanh toán").FontSize(8);
                                c.Item().Text("/QR code").FontSize(7);
                            });

                            qr.Cell().AlignTop().Height(44).Element(e =>
                            {
                                if (bankQrBytes is not null) e.AlignRight().AlignTop().Image(bankQrBytes).FitArea();
                            });
                        });
                    });

                    col.Item().PaddingTop(8).Text("BẢNG BÁO GIÁ DỊCH VỤ (VND) / QUOTATION (VND)").Bold().FontSize(9);

                    col.Item().PaddingTop(10).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(35);
                            cd.RelativeColumn(3.0f);
                            cd.RelativeColumn(0.9f);
                            cd.RelativeColumn(1.15f);
                            cd.RelativeColumn(1.0f);
                            cd.RelativeColumn(1.15f);
                            cd.RelativeColumn(1.0f);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("No.").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Hạng mục/Category").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Số lượng máy/Units").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Đơn giá/Unit price").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Giảm giá/Discount").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Thành tiền/Sum").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Ghi chú/Note").Bold().FontSize(8);
                        });

                        var rowNo = 1;
                        foreach (var i in quote.Items)
                        {
                            t.Cell().Border(0.4f).Padding(3).AlignCenter().Text(rowNo.ToString()).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).Text(i.ServicePrice?.ServiceName ?? string.Empty).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignCenter().Text(i.Quantity.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.UnitPrice.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.DiscountAmount.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.Amount.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).Text(i.Note ?? string.Empty).FontSize(8);
                            rowNo++;
                        }

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text("Tổng cộng/Total Amount").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(subtotal.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text($"Thuế VAT ({vatRate * 100:N0}%)").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(vatAmount.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text("Tổng giá trị thanh toán/Total Amount Payable").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(total.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);
                    });

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                        });
                        tbl.Cell().AlignCenter().Text("Khách hàng\nService User").Bold().FontSize(8);
                        tbl.Cell().AlignCenter().Text("Nhân viên kinh doanh\nSales").Bold().FontSize(8);
                        tbl.Cell().AlignCenter().Text("Thủ Quỹ/Kế Toán\nAccounting").Bold().FontSize(8);
                        tbl.Cell().AlignCenter().Text("Bên cung cấp dịch vụ\nService Provider").Bold().FontSize(8);
                    });

                    col.Item().PaddingTop(22).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                            cd.RelativeColumn();
                        });
                        tbl.Cell().AlignCenter().Text("------------------------------").FontSize(8);
                        tbl.Cell().AlignCenter().Text("------------------------------").FontSize(8);
                        tbl.Cell().AlignCenter().Text("------------------------------").FontSize(8);
                        tbl.Cell().AlignCenter().Text("------------------------------").FontSize(8);
                    });

                    col.Item().PaddingTop(8).Text("Ghi chú/Note:").Bold().Italic().Underline().FontSize(8);
                    col.Item().Text("1. Đơn giá không bao gồm các chi phí khắc phục sự cố hư hỏng và thay thế linh kiện, chi phí thi công khác (Báo giá theo tình hình thực tế). /Unit price excludes repair or replacement costs of components. Any additional work will be quoted separately based on actual inspection.").Italic().FontSize(7);
                    col.Item().Text("2. Thời gian thực hiện dịch vụ kiểm tra và bảo trì tối đa 07 (bảy) ngày làm việc kể từ ngày nhận được yêu cầu của Bên sử dụng dịch vụ thông qua bản cứng/ email/fax. Gói dịch vụ được áp dụng 24/7, áp dụng cả ngày lễ, Tết theo quy định của pháp luật và chủ nhật. /The service shall be completed within 07 working days upon receipt of request via hard copy, email, or fax. Available 24/7, including weekends and public holidays.").Italic().FontSize(7);
                    if (!string.IsNullOrWhiteSpace(quote.Notes))
                        col.Item().Text($"Ghi chú thêm: {quote.Notes}").Italic().FontSize(7);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"{quote.QuoteNo}.pdf");
    }

    public async Task<IActionResult> ExportJpg(int id)
    {
        var quote = await db.Quotes
            .Include(x => x.Customer)
                .ThenInclude(c => c!.ServiceAddresses)
                    .ThenInclude(sa => sa.Address)
                    .ThenInclude(a => a!.Ward)
                    .ThenInclude(w => w!.Province)
            .Include(x => x.Items)
                .ThenInclude(x => x.ServicePrice)
                    .ThenInclude(sp => sp!.Policy)
                        .ThenInclude(p => p!.Unit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        QuestPDF.Settings.License = LicenseType.Community;
        var lineTotal = quote.Items.Sum(x => x.Amount);
        var quoteDiscount = quote.DiscountAmount < 0 ? 0 : quote.DiscountAmount;
        var subtotal = Math.Max(0, lineTotal - quoteDiscount);
        var vatRate = quote.VatRate / 100m;
        var vatAmount = Math.Round(subtotal * vatRate, 0, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        var rootPath = Directory.GetParent(env.ContentRootPath)?.FullName ?? env.ContentRootPath;
        byte[]? LoadImage(params string[] candidates)
        {
            foreach (var relative in candidates)
            {
                var p = Path.Combine(rootPath, relative);
                if (System.IO.File.Exists(p))
                    return System.IO.File.ReadAllBytes(p);
            }
            return null;
        }

        var logoBytes = LoadImage(Path.Combine("document", "logo.png"), Path.Combine("CleantopiaCRM.Web", "wwwroot", "images", "logo.png"));
        var zaloQrBytes = LoadImage(Path.Combine("document", "zalo.jpg"));
        var bankQrBytes = LoadImage(Path.Combine("document", "bank.jpg"));
        var addressText = await BuildFullServiceAddressAsync(quote);
        var phoneText = !string.IsNullOrWhiteSpace(quote.ContactPhone) ? quote.ContactPhone : (quote.Customer?.Phone ?? string.Empty);
        var customerName = !string.IsNullOrWhiteSpace(quote.ContactName) ? quote.ContactName : (quote.Customer?.Name ?? string.Empty);
        var vatRequestText = quote.HasVatInvoice ? "Có" : "Không";
        var orgText = quote.HasVatInvoice
            ? (!string.IsNullOrWhiteSpace(quote.InvoiceCompanyName) ? quote.InvoiceCompanyName! : (quote.Customer?.CompanyName ?? customerName))
            : (quote.Customer?.CompanyName ?? customerName);
        var taxText = quote.HasVatInvoice ? (quote.InvoiceTaxCode ?? string.Empty) : (quote.Customer?.TaxCode ?? string.Empty);
        var emailText = quote.HasVatInvoice ? (quote.InvoiceEmail ?? string.Empty) : (quote.Customer?.Email ?? string.Empty);
        var clientCode = string.IsNullOrWhiteSpace(quote.Customer?.CustomerCode)
            ? $"KH-{quote.CustomerId:00000}"
            : quote.Customer.CustomerCode!;
        var invoiceAddressText = quote.HasVatInvoice ? (quote.InvoiceAddress ?? string.Empty) : string.Empty;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(16);
                page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(9));

                page.Content().Column(col =>
                {
                    col.Item().Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(90);
                            cd.RelativeColumn(2.2f);
                            cd.RelativeColumn(3.6f);
                            cd.ConstantColumn(95);
                        });

                        tbl.Cell().Border(0).Padding(4).Height(74).Element(e =>
                        {
                            if (logoBytes is not null) e.AlignTop().AlignCenter().Image(logoBytes).FitArea();
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text("CÔNG TY TNHH DỊCH VỤ VỆ SINH CLEAN TOPIA").Bold().FontSize(9);
                            c.Item().Text("Địa chỉ: 77 Tạ Hiện, Phường Cát Lái, TP.HCM, Việt Nam").FontSize(8);
                            c.Item().Text("MST: 0318114168   Hotline: 090.769.1010").FontSize(8);
                            c.Item().Text("Email: info@cleantopia.vn    Website: https://cleantopia.vn/").FontSize(8);
                        });
                        tbl.Cell().Border(0).Padding(4).AlignTop().Column(c =>
                        {
                            c.Item().AlignCenter().Text("ĐƠN ĐĂNG KÝ SỬ DỤNG DỊCH VỤ").Bold().FontSize(12);
                            c.Item().AlignCenter().Text("SERVICE APPLICATION FORM").Bold().FontSize(11);
                            c.Item().AlignCenter().Text($"PLHĐ số/Order No: {quote.QuoteNo}   Ngày/Date: {quote.QuoteDate:dd/MM/yyyy}").Italic().FontSize(8);
                            c.Item().AlignCenter().Text($"Mã KH/Client code: {clientCode}").Italic().FontSize(8);
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().AlignCenter().Text("Hotline Zalo").FontSize(8);
                            c.Item().PaddingTop(2).Height(58).Element(e =>
                            {
                                if (zaloQrBytes is not null) e.AlignTop().AlignCenter().Image(zaloQrBytes).FitArea();
                            });
                        });
                    });

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(1);
                        });

                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text($"Công Ty/ Organizations                 : {orgText}");
                            c.Item().Text($"Ông/Bà (Đại diện) Mr/Ms (Represented by) : {customerName}");
                            c.Item().Text($"Địa chỉ/ Address                       : {addressText}");
                            c.Item().Text($"Yêu cầu xuất hóa đơn/ VAT invoice request : {vatRequestText}");
                            c.Item().Text($"Địa chỉ xuất hóa đơn/ Invoice address    : {invoiceAddressText}");
                            c.Item().Text($"Ngày đặt lịch hẹn/ Reservation date    : {quote.QuoteDate:dd/MM/yyyy}");
                        });
                        tbl.Cell().Border(0).Padding(4).Column(c =>
                        {
                            c.Item().Text($"MST/ Tax Code: {taxText}");
                            c.Item().Text("Chức vụ/ Position: ");
                            c.Item().Text($"SDT/ Phone no.: {phoneText}");
                            c.Item().Text($"Email: {emailText}");
                            c.Item().Text($"Thông tin liên lạc/ Contact person: {quote.ContactName ?? customerName}");
                            c.Item().Text($"Thời gian/ Time: {DateTime.Now:hh:mm tt}");
                        });
                    });

                    col.Item().PaddingTop(6).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2.4f);
                            cd.RelativeColumn(3.2f);
                            cd.RelativeColumn(3.0f);
                            cd.ConstantColumn(120);
                        });

                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().Text("Hình thức và thông tin thanh toán").Bold().FontSize(8);
                            c.Item().Text("/Payment method and Information").FontSize(7);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().PaddingTop(2).Text("☐ Tiền mặt/Cash").FontSize(10);
                            c.Item().Text("☐ Chuyển khoản/Bank Transfer").FontSize(10);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Column(c =>
                        {
                            c.Item().Text("Số tài khoản/Bank Acc no.: 003 1811 4168").Bold().FontSize(8);
                            c.Item().Text("Ngân hàng/Bank name:").FontSize(8);
                            c.Item().Text("Ngân hàng TMCP Tiên Phong - TP Bank CN Quận 04").Bold().FontSize(8);
                        });
                        tbl.Cell().Border(0.4f).Padding(4).Table(qr =>
                        {
                            qr.ColumnsDefinition(qcd =>
                            {
                                qcd.RelativeColumn(1.5f);
                                qcd.RelativeColumn(1f);
                            });

                            qr.Cell().AlignTop().Column(c =>
                            {
                                c.Item().Text("QR Thanh toán").FontSize(8);
                                c.Item().Text("/QR code").FontSize(7);
                            });

                            qr.Cell().AlignTop().Height(44).Element(e =>
                            {
                                if (bankQrBytes is not null) e.AlignRight().AlignTop().Image(bankQrBytes).FitArea();
                            });
                        });
                    });

                    col.Item().PaddingTop(8).Text("BẢNG BÁO GIÁ DỊCH VỤ (VND) / QUOTATION (VND)").Bold().FontSize(9);

                    col.Item().PaddingTop(10).Table(t =>
                    {
                        t.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(35);
                            cd.RelativeColumn(3.0f);
                            cd.RelativeColumn(0.9f);
                            cd.RelativeColumn(1.15f);
                            cd.RelativeColumn(1.0f);
                            cd.RelativeColumn(1.15f);
                            cd.RelativeColumn(1.0f);
                        });
                        t.Header(h =>
                        {
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("No.").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Hạng mục/Category").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Số lượng máy/Units").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Đơn giá/Unit price").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Giảm giá/Discount").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Thành tiền/Sum").Bold().FontSize(8);
                            h.Cell().Border(0.4f).Padding(3).AlignCenter().Text("Ghi chú/Note").Bold().FontSize(8);
                        });

                        var rowNo = 1;
                        foreach (var i in quote.Items)
                        {
                            t.Cell().Border(0.4f).Padding(3).AlignCenter().Text(rowNo.ToString()).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).Text(i.ServicePrice?.ServiceName ?? string.Empty).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignCenter().Text(i.Quantity.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.UnitPrice.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.DiscountAmount.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).AlignRight().Text(i.Amount.ToString("N0")).FontSize(8);
                            t.Cell().Border(0.4f).Padding(3).Text(i.Note ?? string.Empty).FontSize(8);
                            rowNo++;
                        }

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text("Tổng cộng/Total Amount").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(subtotal.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text($"Thuế VAT ({vatRate * 100:N0}%)").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(vatAmount.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);

                        t.Cell().ColumnSpan(5).Border(0.4f).Padding(3).AlignRight().Text("Tổng giá trị thanh toán/Total Amount Payable").Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).AlignRight().Text(total.ToString("N0")).Bold().FontSize(8);
                        t.Cell().Border(0.4f).Padding(3).Text(string.Empty);
                    });

                    col.Item().PaddingTop(8).Text("Ghi chú/Note:").Bold().Italic().Underline().FontSize(8);
                    col.Item().Text("1. Đơn giá không bao gồm các chi phí khắc phục sự cố hư hỏng và thay thế linh kiện, chi phí thi công khác (Báo giá theo tình hình thực tế). /Unit price excludes repair or replacement costs of components. Any additional work will be quoted separately based on actual inspection.").Italic().FontSize(7);
                    col.Item().Text("2. Thời gian thực hiện dịch vụ kiểm tra và bảo trì tối đa 07 (bảy) ngày làm việc kể từ ngày nhận được yêu cầu của Bên sử dụng dịch vụ thông qua bản cứng/ email/fax. Gói dịch vụ được áp dụng 24/7, áp dụng cả ngày lễ, Tết theo quy định của pháp luật và chủ nhật. /The service shall be completed within 07 working days upon receipt of request via hard copy, email, or fax. Available 24/7, including weekends and public holidays.").Italic().FontSize(7);
                    if (!string.IsNullOrWhiteSpace(quote.Notes))
                        col.Item().Text($"Ghi chú thêm: {quote.Notes}").Italic().FontSize(7);
                });
            });
        });

        var images = document.GenerateImages(new ImageGenerationSettings
        {
            RasterDpi = 220,
            ImageFormat = ImageFormat.Jpeg
        });
        var firstImage = images.FirstOrDefault();
        if (firstImage is null || firstImage.Length == 0)
            return NotFound();

        return File(firstImage, "image/jpeg", $"{quote.QuoteNo}.jpg");
    }

    public async Task<IActionResult> ExportWord(int id)
    {
        var quote = await db.Quotes
            .Include(x => x.Customer)
            .Include(x => x.Items)
                .ThenInclude(x => x.ServicePrice)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (quote is null) return NotFound();

        var lineTotal = quote.Items.Sum(x => x.Amount);
        var quoteDiscount = quote.DiscountAmount < 0 ? 0 : quote.DiscountAmount;
        var subtotal = Math.Max(0, lineTotal - quoteDiscount);
        var vat = Math.Round(subtotal * (quote.VatRate / 100m), 0, MidpointRounding.AwayFromZero);
        var total = subtotal + vat;
        var clientCode = string.IsNullOrWhiteSpace(quote.Customer?.CustomerCode)
            ? $"KH-{quote.CustomerId:00000}"
            : quote.Customer.CustomerCode!;
        var phoneText = !string.IsNullOrWhiteSpace(quote.ContactPhone) ? quote.ContactPhone : (quote.Customer?.Phone ?? string.Empty);
        var customerName = !string.IsNullOrWhiteSpace(quote.ContactName) ? quote.ContactName : (quote.Customer?.Name ?? string.Empty);
        var addressText = await BuildFullServiceAddressAsync(quote);
        var vatRequestText = quote.HasVatInvoice ? "Có" : "Không";
        var orgText = quote.HasVatInvoice
            ? (!string.IsNullOrWhiteSpace(quote.InvoiceCompanyName) ? quote.InvoiceCompanyName! : (quote.Customer?.CompanyName ?? customerName))
            : (quote.Customer?.CompanyName ?? customerName);
        var taxText = quote.HasVatInvoice ? (quote.InvoiceTaxCode ?? string.Empty) : (quote.Customer?.TaxCode ?? string.Empty);
        var emailText = quote.HasVatInvoice ? (quote.InvoiceEmail ?? string.Empty) : (quote.Customer?.Email ?? string.Empty);
        var validUntilText = quote.ValidUntil?.ToString("dd/MM/yyyy") ?? string.Empty;
        var invoiceAddressText = quote.HasVatInvoice ? (quote.InvoiceAddress ?? string.Empty) : string.Empty;

        var rootPath = Directory.GetParent(env.ContentRootPath)?.FullName ?? env.ContentRootPath;
        string? ToBase64Image(int? maxWidth, int? maxHeight, bool keepAspectRatio, bool forceWhiteBackground, params string[] candidates)
        {
            foreach (var relative in candidates)
            {
                var p = Path.Combine(rootPath, relative);
                if (!System.IO.File.Exists(p)) continue;
                try
                {
                    using var srcImage = SixLabors.ImageSharp.Image.Load(p);
                    var targetW = maxWidth ?? srcImage.Width;
                    var targetH = maxHeight ?? srcImage.Height;
                    if (targetW <= 0) targetW = srcImage.Width;
                    if (targetH <= 0) targetH = srcImage.Height;

                    srcImage.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(targetW, targetH),
                        Mode = keepAspectRatio ? ResizeMode.Max : ResizeMode.Stretch
                    }));

                    if (forceWhiteBackground)
                        srcImage.Mutate(x => x.BackgroundColor(SixLabors.ImageSharp.Color.White));

                    using var ms = new MemoryStream();
                    srcImage.Save(ms, new PngEncoder());
                    return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
                catch
                {
                    var ext = Path.GetExtension(p).ToLowerInvariant();
                    var mime = ext == ".png" ? "image/png" : "image/jpeg";
                    var bytes = System.IO.File.ReadAllBytes(p);
                    return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
            }
            return null;
        }

        var logoDataUrl = ToBase64Image(220, 130, true, true, Path.Combine("document", "logo.png"), Path.Combine("CleantopiaCRM.Web", "wwwroot", "images", "logo.png"));
        var zaloQrDataUrl = ToBase64Image(64, 64, false, false, Path.Combine("document", "zalo.jpg"));
        var bankQrDataUrl = ToBase64Image(58, 58, false, false, Path.Combine("document", "bank.jpg"));

        var sb = new StringBuilder();
        sb.Append("<html xmlns:o='urn:schemas-microsoft-com:office:office' xmlns:w='urn:schemas-microsoft-com:office:word' xmlns='http://www.w3.org/TR/REC-html40'><head><meta charset='utf-8'>");
        sb.Append("<!--[if gte mso 9]><xml><w:WordDocument><w:View>Print</w:View><w:Zoom>90</w:Zoom><w:DoNotOptimizeForBrowser/></w:WordDocument></xml><![endif]-->");
        sb.Append("<style>");
        sb.Append("@page Section1 { size:841.9pt 595.3pt; mso-page-orientation:landscape; margin:14pt 14pt 14pt 14pt; } ");
        sb.Append("body{font-family:'Times New Roman';font-size:10pt;margin:0;} ");
        sb.Append("table{border-collapse:collapse;width:100%;table-layout:fixed;} td,th{border:0.8px solid #555;padding:4px;vertical-align:top;word-wrap:break-word;} ");
        sb.Append(".nb td,.nb th{border:none;} .r{text-align:right;} .c{text-align:center;} .b{font-weight:bold;} ");
        sb.Append("div.Section1{page:Section1;} ");
        sb.Append(".small{font-size:10pt;} .xsmall{font-size:9pt;} .tiny{font-size:8pt;} .line{letter-spacing:1px;} .quote-table{font-size:8pt;} ");
        sb.Append("</style></head><body>");
        sb.Append("<div class='Section1'>");

        sb.Append("<table class='nb' style='margin-bottom:6px;'><colgroup><col style='width:10%'><col style='width:32%'><col style='width:43%'><col style='width:15%'></colgroup><tr>");
        sb.Append("<td>");
        if (!string.IsNullOrWhiteSpace(logoDataUrl))
            sb.Append($"<img src='{logoDataUrl}' width='190' height='110' style='display:block;width:142pt;height:82pt;mso-width-alt:2840;mso-height-alt:1640;'/>");
        sb.Append("</td>");
        sb.Append("<td class='small'>");
        sb.Append("<div class='b'>CÔNG TY TNHH DỊCH VỤ VỆ SINH CLEAN TOPIA</div>");
        sb.Append("<div>Địa chỉ: 77 Tạ Hiện, Phường Cát Lái, TP.HCM, Việt Nam</div>");
        sb.Append("<div>MST: 0318114168&nbsp;&nbsp; Hotline: 090.769.1010</div>");
        sb.Append("<div>Email: info@cleantopia.vn&nbsp;&nbsp; Website: https://cleantopia.vn/</div>");
        sb.Append("</td>");
        sb.Append("<td class='c'>");
        sb.Append("<div class='b' style='font-size:13pt;line-height:1.1;'>ĐƠN ĐĂNG KÝ SỬ DỤNG DỊCH VỤ</div>");
        sb.Append("<div class='b' style='font-size:11.5pt;line-height:1.1;'>SERVICE APPLICATION FORM</div>");
        sb.Append($"<div class='small'><i>PLHĐ số/Order No: {quote.QuoteNo} &nbsp; Ngày/Date: {quote.QuoteDate:dd/MM/yyyy}</i></div>");
        sb.Append($"<div class='small'><i>Mã KH/Client code: {clientCode}</i></div>");
        sb.Append("</td>");
        sb.Append("<td class='c'>");
        sb.Append("<div class='small'>Hotline Zalo</div>");
        if (!string.IsNullOrWhiteSpace(zaloQrDataUrl))
            sb.Append($"<img src='{zaloQrDataUrl}' width='56' height='56' style='display:inline-block;width:42pt;height:42pt;mso-width-alt:840;mso-height-alt:840;'/>");
        else
            sb.Append("<div class='xsmall' style='margin-top:18px;'>[Thiếu ảnh QR Zalo]</div>");
        sb.Append("</td>");
        sb.Append("</tr></table>");

        sb.Append("<table style='margin-bottom:6px;'><colgroup><col style='width:70%'><col style='width:30%'></colgroup><tr>");
        sb.Append("<td>");
        sb.Append($"<div>Công Ty/ Organizations : {orgText}</div>");
        sb.Append($"<div>Ông/Bà (Đại diện) Mr/Ms (Represented by) : {customerName}</div>");
        sb.Append($"<div>Địa chỉ/ Address : {addressText}</div>");
        sb.Append($"<div>Yêu cầu xuất hóa đơn/ VAT invoice request : {vatRequestText}</div>");
        sb.Append($"<div>Địa chỉ xuất hóa đơn/ Invoice address : {invoiceAddressText}</div>");
        sb.Append($"<div>Ngày đặt lịch hẹn/ Reservation date : {quote.QuoteDate:dd/MM/yyyy}</div>");
        sb.Append("</td>");
        sb.Append("<td>");
        sb.Append($"<div>MST/ Tax Code: {taxText}</div>");
        sb.Append("<div>Chức vụ/ Position: </div>");
        sb.Append($"<div>SDT/ Phone no.: {phoneText}</div>");
        sb.Append($"<div>Email: {emailText}</div>");
        sb.Append($"<div>Thông tin liên lạc/ Contact person: {quote.ContactName ?? customerName}</div>");
        sb.Append($"<div>Thời gian/ Time: {DateTime.Now:hh:mm tt}</div>");
        sb.Append("</td>");
        sb.Append("</tr></table>");

        sb.Append("<table style='margin-bottom:8px;'><colgroup><col style='width:24%'><col style='width:31%'><col style='width:29%'><col style='width:16%'></colgroup><tr>");
        sb.Append("<td>");
        sb.Append("<div class='b small'>Hình thức và thông tin thanh toán</div>");
        sb.Append("<div class='xsmall'>/Payment method and Information</div>");
        sb.Append("</td>");
        sb.Append("<td>");
        sb.Append("<div>☐ Tiền mặt/Cash</div>");
        sb.Append("<div>☐ Chuyển khoản/Bank Transfer</div>");
        sb.Append("</td>");
        sb.Append("<td>");
        sb.Append("<div class='b'>Số tài khoản/Bank Acc no.: 003 1811 4168</div>");
        sb.Append("<div>Ngân hàng/Bank name:</div>");
        sb.Append("<div class='b'>Ngân hàng TMCP Tiên Phong - TP Bank CN Quận 04</div>");
        sb.Append("</td>");
        sb.Append("<td>");
        sb.Append("<table class='nb'><tr><td style='border:none;'>");
        sb.Append("<div>QR Thanh toán</div><div class='xsmall'>/QR code</div>");
        sb.Append("</td><td style='border:none;' class='r'>");
        if (!string.IsNullOrWhiteSpace(bankQrDataUrl))
            sb.Append($"<img src='{bankQrDataUrl}' width='52' height='52' style='display:inline-block;width:39pt;height:39pt;mso-width-alt:780;mso-height-alt:780;'/>");
        else
            sb.Append("<div class='xsmall' style='margin-top:16px;'>[Thiếu ảnh QR thanh toán]</div>");
        sb.Append("</td></tr></table>");
        sb.Append("</td>");
        sb.Append("</tr></table>");

        sb.Append("<div class='b' style='font-size:9pt;margin:8px 0 6px 0;'>BẢNG BÁO GIÁ DỊCH VỤ (VND) / QUOTATION (VND)</div>");
        sb.Append("<table class='quote-table'><thead><tr><th style='width:40px;'>No.</th><th>Hạng mục/Category</th><th style='width:90px;'>Số lượng máy/Units</th><th style='width:120px;'>Đơn giá/Unit price</th><th style='width:110px;'>Giảm giá/Discount</th><th style='width:120px;'>Thành tiền/Sum</th><th style='width:110px;'>Ghi chú/Note</th></tr></thead><tbody>");
        var idx = 1;
        foreach (var i in quote.Items)
        {
            sb.Append($"<tr><td class='c'>{idx++}</td><td>{i.ServicePrice?.ServiceName}</td><td class='c'>{i.Quantity:N0}</td><td class='r'>{i.UnitPrice:N0}</td><td class='r'>{i.DiscountAmount:N0}</td><td class='r'>{i.Amount:N0}</td><td>{i.Note}</td></tr>");
        }
        sb.Append($"<tr><td colspan='5' class='r b'>Tổng cộng/Total Amount</td><td class='r b'>{lineTotal:N0}</td><td></td></tr>");
        if (quoteDiscount > 0)
            sb.Append($"<tr><td colspan='5' class='r b'>Giảm giá chung/Quote discount</td><td class='r b'>{quoteDiscount:N0}</td><td></td></tr>");
        sb.Append($"<tr><td colspan='5' class='r b'>Thuế VAT ({quote.VatRate:0.##}%)</td><td class='r b'>{vat:N0}</td><td></td></tr>");
        sb.Append($"<tr><td colspan='5' class='r b'>Tổng giá trị thanh toán/Total Amount Payable</td><td class='r b'>{total:N0}</td><td></td></tr>");
        sb.Append("</tbody></table>");

        sb.Append("<table class='nb' style='margin-top:8px;'><tr>");
        sb.Append("<td class='c b' style='width:25%;'>Khách hàng<br/>Service User</td>");
        sb.Append("<td class='c b' style='width:25%;'>Nhân viên kinh doanh<br/>Sales</td>");
        sb.Append("<td class='c b' style='width:25%;'>Thủ Quỹ/Kế Toán<br/>Accounting</td>");
        sb.Append("<td class='c b' style='width:25%;'>Bên cung cấp dịch vụ<br/>Service Provider</td>");
        sb.Append("</tr><tr>");
        sb.Append("<td class='c' style='padding-top:34px;'>------------------------------</td>");
        sb.Append("<td class='c' style='padding-top:34px;'>------------------------------</td>");
        sb.Append("<td class='c' style='padding-top:34px;'>------------------------------</td>");
        sb.Append("<td class='c' style='padding-top:34px;'>------------------------------</td>");
        sb.Append("</tr></table>");

        sb.Append("<p style='margin-top:8px;'><b><i><u>Ghi chú/Note:</u></i></b></p>");
        sb.Append("<p class='tiny'><i>1. Đơn giá không bao gồm các chi phí khắc phục sự cố hư hỏng và thay thế linh kiện, chi phí thi công khác (Báo giá theo tình hình thực tế). /Unit price excludes repair or replacement costs of components. Any additional work will be quoted separately based on actual inspection.</i></p>");
        sb.Append("<p class='tiny'><i>2. Thời gian thực hiện dịch vụ kiểm tra và bảo trì tối đa 07 (bảy) ngày làm việc kể từ ngày nhận được yêu cầu của Bên sử dụng dịch vụ thông qua bản cứng/ email/fax. Gói dịch vụ được áp dụng 24/7, áp dụng cả ngày lễ, Tết theo quy định của pháp luật và chủ nhật. /The service shall be completed within 07 working days upon receipt of request via hard copy, email, or fax. Available 24/7, including weekends and public holidays.</i></p>");
        if (!string.IsNullOrWhiteSpace(quote.Notes))
            sb.Append($"<p class='tiny'><i>Ghi chú thêm: {quote.Notes}</i></p>");
        sb.Append("</div>");
        sb.Append("</body></html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "application/msword", $"{quote.QuoteNo}.doc");
    }

    [HttpGet]
    public async Task<IActionResult> CustomerAddresses(int customerId)
    {
        var addresses = await db.CustomerServiceAddresses
            .Include(x => x.Address)
                .ThenInclude(a => a!.Ward)
                .ThenInclude(w => w!.Province)
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                id = x.Id,
                text = ((x.Address != null ? (x.Address.HouseNumber + " " + x.Address.Street + ", " + (x.Address.Ward != null ? x.Address.Ward.WardName : "") + ", " + (x.Address.Ward != null && x.Address.Ward.Province != null ? x.Address.Ward.Province.ProvinceName : "")) : "")).Trim(),
                addressLine = x.Address != null ? ((x.Address.HouseNumber + " " + x.Address.Street).Trim()) : "",
                provinceId = x.Address != null ? (int?)x.Address.ProvinceId : null,
                wardId = x.Address != null ? (int?)x.Address.WardId : null,
                contactName = x.ContactName,
                contactPhone = x.ContactPhone
            })
            .ToListAsync();

        return Json(addresses);
    }

    [HttpPost]
    public async Task<IActionResult> QuickCreateCustomer([FromBody] QuickCustomerRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Vui lòng nhập tên khách hàng." });
        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại." });

        var countryId = req.CountryId.GetValueOrDefault();
        if (countryId <= 0)
        {
            countryId = await db.Countries
                .Where(x => x.Code == "VN" || x.Name == "Vietnam" || x.Name == "Viet Nam")
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
            if (countryId == 0)
                countryId = await db.Countries.OrderBy(x => x.Id).Select(x => x.Id).FirstOrDefaultAsync();
        }

        var sourceId = req.CustomerSourceId;
        if (!sourceId.HasValue || sourceId.Value <= 0)
            sourceId = await db.CustomerSources.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync();

        var typeId = req.CustomerTypeId;
        if (!typeId.HasValue || typeId.Value <= 0)
            typeId = await db.CustomerTypes.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Select(x => (int?)x.Id).FirstOrDefaultAsync();

        if (!sourceId.HasValue || !typeId.HasValue)
            return BadRequest(new { message = "Thiếu cấu hình nguồn khách hoặc loại khách." });

        var customer = new Customer
        {
            Name = req.Name.Trim(),
            Phone = req.Phone.Trim(),
            TaxCode = string.IsNullOrWhiteSpace(req.TaxCode) ? null : req.TaxCode.Trim(),
            Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            CountryId = countryId,
            CustomerSourceId = sourceId,
            CustomerTypeId = typeId,
            CreatedAt = DateTime.Now
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        customer.CustomerCode = $"KH-{customer.Id:00000}";
        await db.SaveChangesAsync();

        return Json(new
        {
            id = customer.Id,
            text = customer.Name,
            phone = customer.Phone ?? "",
            taxCode = customer.TaxCode ?? "",
            customerCode = customer.CustomerCode ?? ""
        });
    }

    private async Task LoadLookup()
    {
        ViewBag.Customers = await db.Customers.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Services = await db.ServicePrices.Where(x => x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.ServiceName).ToListAsync();
        ViewBag.Countries = await db.Countries.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Sources = await db.CustomerSources.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        ViewBag.Types = await db.CustomerTypes.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();

        var hcmProvinceId = await db.GhnProvinces
            .Where(x => x.ProvinceName.Contains("Hồ Chí Minh") || x.ProvinceName.Contains("Ho Chi Minh"))
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();
        var defaultProvinceId = hcmProvinceId ?? await db.GhnProvinces.OrderBy(x => x.ProvinceName).Select(x => x.Id).FirstOrDefaultAsync();
        ViewBag.Provinces = await db.GhnProvinces.OrderBy(x => x.ProvinceName).ToListAsync();
        ViewBag.DefaultProvinceId = defaultProvinceId;
        ViewBag.Wards = await db.GhnWards.Where(x => x.ProvinceId == defaultProvinceId).OrderBy(x => x.WardName).ToListAsync();
    }

    private async Task<string> BuildFullServiceAddressAsync(Quote quote)
    {
        var baseText = (quote.ServiceAddressText ?? string.Empty).Trim();
        var wardName = string.Empty;
        var provinceName = string.Empty;

        if (quote.ServiceWardId.HasValue && quote.ServiceWardId.Value > 0)
        {
            wardName = await db.GhnWards
                .Where(x => x.Id == quote.ServiceWardId.Value)
                .Select(x => x.WardName)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        if (quote.ServiceProvinceId.HasValue && quote.ServiceProvinceId.Value > 0)
        {
            provinceName = await db.GhnProvinces
                .Where(x => x.Id == quote.ServiceProvinceId.Value)
                .Select(x => x.ProvinceName)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(baseText)) parts.Add(baseText);
        if (!string.IsNullOrWhiteSpace(wardName) && !baseText.Contains(wardName, StringComparison.OrdinalIgnoreCase)) parts.Add(wardName);
        if (!string.IsNullOrWhiteSpace(provinceName) && !baseText.Contains(provinceName, StringComparison.OrdinalIgnoreCase)) parts.Add(provinceName);
        return string.Join(", ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void ApplyQuoteTotals(Quote quote)
    {
        var lineTotal = quote.Items.Sum(x => x.Amount);
        var quoteDiscount = quote.DiscountAmount < 0 ? 0 : quote.DiscountAmount;
        var subtotal = Math.Max(0, lineTotal - quoteDiscount);
        var vatRate = quote.VatRate < 0 ? 0 : quote.VatRate;
        var vatAmount = Math.Round(subtotal * (vatRate / 100m), 0, MidpointRounding.AwayFromZero);
        var total = subtotal + vatAmount;

        quote.SubtotalAmount = subtotal;
        quote.VatAmount = vatAmount;
        quote.TotalAmount = total;
    }

    private async Task SaveQuoteAddressAndInvoiceForReuseAsync(Quote quote)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Id == quote.CustomerId);
        if (customer is null) return;

        if (quote.ServiceAddressId.HasValue && quote.ServiceAddressId.Value > 0)
        {
            var existing = await db.CustomerServiceAddresses
                .Include(x => x.Address)
                .FirstOrDefaultAsync(x => x.Id == quote.ServiceAddressId.Value && x.CustomerId == quote.CustomerId && x.IsActive);
            if (existing?.Address is not null)
            {
                quote.ServiceProvinceId = existing.Address.ProvinceId;
                quote.ServiceWardId = existing.Address.WardId;
                quote.ServiceAddressText ??= $"{existing.Address.HouseNumber} {existing.Address.Street}".Trim();
                quote.ContactName ??= existing.ContactName;
                quote.ContactPhone ??= existing.ContactPhone;
            }
        }

        if (quote.HasVatInvoice)
        {
            customer.IsBusiness = true;
            customer.CompanyName = quote.InvoiceCompanyName;
            customer.TaxCode = quote.InvoiceTaxCode;
            customer.BillingAddress = quote.InvoiceAddress;
            customer.BillingEmail = quote.InvoiceEmail;
            customer.BillingReceiver = quote.InvoiceReceiver;
            if (!string.IsNullOrWhiteSpace(quote.ContactPhone))
                customer.BillingPhone = quote.ContactPhone;
        }

        if (quote.ServiceAddressId.HasValue && quote.ServiceAddressId.Value > 0)
            return;
        if (!quote.ServiceProvinceId.HasValue || !quote.ServiceWardId.HasValue || string.IsNullOrWhiteSpace(quote.ServiceAddressText))
            return;

        var fullLine = quote.ServiceAddressText.Trim();
        var houseNumber = fullLine;
        var street = "";
        var commaIndex = fullLine.IndexOf(',');
        if (commaIndex > 0)
        {
            houseNumber = fullLine[..commaIndex].Trim();
            street = fullLine[(commaIndex + 1)..].Trim();
        }
        else
        {
            var spaceIndex = fullLine.IndexOf(' ');
            if (spaceIndex > 0)
            {
                houseNumber = fullLine[..spaceIndex].Trim();
                street = fullLine[(spaceIndex + 1)..].Trim();
            }
        }
        if (string.IsNullOrWhiteSpace(street))
            street = houseNumber;

        var address = await db.Addresses.FirstOrDefaultAsync(x =>
            x.HouseNumber == houseNumber &&
            x.Street == street &&
            x.ProvinceId == quote.ServiceProvinceId.Value &&
            x.WardId == quote.ServiceWardId.Value);

        if (address is null)
        {
            address = new Address
            {
                HouseNumber = houseNumber,
                Street = street,
                ProvinceId = quote.ServiceProvinceId.Value,
                WardId = quote.ServiceWardId.Value
            };
            db.Addresses.Add(address);
            await db.SaveChangesAsync();
        }

        var csAddress = await db.CustomerServiceAddresses.FirstOrDefaultAsync(x =>
            x.CustomerId == quote.CustomerId &&
            x.AddressId == address.Id &&
            x.IsActive);

        if (csAddress is null)
        {
            csAddress = new CustomerServiceAddress
            {
                CustomerId = quote.CustomerId,
                AddressId = address.Id,
                ContactName = string.IsNullOrWhiteSpace(quote.ContactName) ? customer.Name : quote.ContactName!,
                ContactPhone = quote.ContactPhone,
                HasOwnInvoiceInfo = quote.HasVatInvoice,
                InvoiceCompanyName = quote.InvoiceCompanyName,
                InvoiceTaxCode = quote.InvoiceTaxCode,
                InvoiceAddress = quote.InvoiceAddress,
                InvoiceEmail = quote.InvoiceEmail,
                InvoiceReceiver = quote.InvoiceReceiver,
                IsActive = true
            };
            db.CustomerServiceAddresses.Add(csAddress);
            await db.SaveChangesAsync();
        }
        else
        {
            csAddress.ContactName = string.IsNullOrWhiteSpace(quote.ContactName) ? csAddress.ContactName : quote.ContactName!;
            csAddress.ContactPhone = quote.ContactPhone;
            if (quote.HasVatInvoice)
            {
                csAddress.HasOwnInvoiceInfo = true;
                csAddress.InvoiceCompanyName = quote.InvoiceCompanyName;
                csAddress.InvoiceTaxCode = quote.InvoiceTaxCode;
                csAddress.InvoiceAddress = quote.InvoiceAddress;
                csAddress.InvoiceEmail = quote.InvoiceEmail;
                csAddress.InvoiceReceiver = quote.InvoiceReceiver;
            }
            await db.SaveChangesAsync();
        }

        quote.ServiceAddressId = csAddress.Id;
    }

    private void ValidateQuoteItems(int[] itemServicePriceId, decimal[] itemQuantity, decimal[] itemUnitPrice, decimal[] itemDiscountAmount)
    {
        if (itemServicePriceId is null || itemServicePriceId.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng thêm ít nhất một dịch vụ.");
            return;
        }

        for (var i = 0; i < itemServicePriceId.Length; i++)
        {
            var serviceId = itemServicePriceId[i];
            var qty = i < itemQuantity.Length ? itemQuantity[i] : 0;
            var price = i < itemUnitPrice.Length ? itemUnitPrice[i] : 0;
            var discount = i < itemDiscountAmount.Length ? itemDiscountAmount[i] : 0;

            if (serviceId <= 0)
                ModelState.AddModelError("", $"Dòng {i + 1}: Vui lòng chọn dịch vụ.");
            if (qty <= 0)
                ModelState.AddModelError("", $"Dòng {i + 1}: Số lượng phải lớn hơn 0.");
            if (price <= 0)
                ModelState.AddModelError("", $"Dòng {i + 1}: Đơn giá phải lớn hơn 0.");
            if (discount < 0)
                ModelState.AddModelError("", $"Dòng {i + 1}: Giảm giá không hợp lệ.");
            if ((qty * price) < discount)
                ModelState.AddModelError("", $"Dòng {i + 1}: Giảm giá không được lớn hơn thành tiền.");
        }
    }

    private List<QuoteItem> BuildQuoteItems(int[] itemServicePriceId, decimal[] itemQuantity, decimal[] itemUnitPrice, decimal[] itemDiscountAmount, string[] itemNote)
    {
        var items = new List<QuoteItem>();
        for (var i = 0; i < itemServicePriceId.Length; i++)
        {
            var serviceId = itemServicePriceId[i];
            var qty = i < itemQuantity.Length ? itemQuantity[i] : 0;
            var price = i < itemUnitPrice.Length ? itemUnitPrice[i] : 0;
            var discount = i < itemDiscountAmount.Length ? itemDiscountAmount[i] : 0;
            var note = i < itemNote.Length ? itemNote[i] : null;

            if (serviceId <= 0 || qty <= 0 || price <= 0)
                continue;

            items.Add(new QuoteItem
            {
                ServicePriceId = serviceId,
                Quantity = qty,
                UnitPrice = price,
                DiscountAmount = discount < 0 ? 0 : discount,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            });
        }

        return items;
    }
}

public class QuickCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public int? CountryId { get; set; }
    public int? CustomerSourceId { get; set; }
    public int? CustomerTypeId { get; set; }
}

