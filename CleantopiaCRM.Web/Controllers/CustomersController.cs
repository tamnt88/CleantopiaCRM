using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class CustomersController(AppDbContext db) : Controller
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
            "<script>window.parent && window.parent.CRM && window.parent.CRM.closeCustomerModal(true);</script>",
            "text/html");
    }

    public async Task<IActionResult> Index(string? q, int? sourceId, int? typeId, int? countryId, int page = 1, int pageSize = 10)
    {
        var query = db.Customers
            .Include(x => x.Country)
            .Include(x => x.CustomerSource)
            .Include(x => x.CustomerType)
            .Include(x => x.ServiceAddresses.Where(a => a.IsActive))
                .ThenInclude(a => a.Address)
                .ThenInclude(a => a!.Ward)
                .ThenInclude(w => w!.Province)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.Name.Contains(q) || (x.Phone ?? "").Contains(q) || (x.Email ?? "").Contains(q));
        }

        if (sourceId.HasValue) query = query.Where(x => x.CustomerSourceId == sourceId.Value);
        if (typeId.HasValue) query = query.Where(x => x.CustomerTypeId == typeId.Value);
        if (countryId.HasValue) query = query.Where(x => x.CountryId == countryId.Value);

        var items = await query
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        var total = items.Count;

        ViewBag.SourceId = sourceId;
        ViewBag.TypeId = typeId;
        ViewBag.CountryId = countryId;
        ViewBag.Countries = await db.Countries.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Sources = await db.CustomerSources.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        ViewBag.Types = await db.CustomerTypes.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();

        return View(new PagedResult<Customer> { Items = items, Page = 1, PageSize = total == 0 ? 10 : total, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        var defaultCountryId = await GetDefaultCountryIdAsync();
        await LoadLookup();
        ViewBag.IsModal = IsModalRequest();
        return View(new Customer
        {
            CreatedAt = DateTime.Now,
            CountryId = defaultCountryId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer item)
    {
        ValidateCreateRequiredFields(item);

        if (!ModelState.IsValid)
        {
            await LoadLookup();
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        item.CreatedAt = DateTime.Now;
        db.Customers.Add(item);
        await db.SaveChangesAsync();
        if (string.IsNullOrWhiteSpace(item.CustomerCode))
        {
            item.CustomerCode = $"KH-{item.Id:00000}";
            await db.SaveChangesAsync();
        }

        return ModalSuccessResult();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        await LoadLookup();
        ViewBag.IsModal = IsModalRequest();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Customer item)
    {
        var old = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();

        ValidateCreateRequiredFields(item);
        if (!ModelState.IsValid)
        {
            await LoadLookup();
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        old.Name = item.Name;
        old.Phone = item.Phone;
        old.Email = item.Email;
        old.CountryId = item.CountryId;
        old.CustomerSourceId = item.CustomerSourceId;
        old.CustomerTypeId = item.CustomerTypeId;
        old.Notes = item.Notes;

        await db.SaveChangesAsync();
        return ModalSuccessResult();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,DieuPhoi")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Customers.FindAsync(id);
        if (item is not null)
        {
            db.Customers.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookup()
    {
        ViewBag.Countries = await db.Countries.OrderBy(x => x.Name).ToListAsync();
        ViewBag.Sources = await db.CustomerSources.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        ViewBag.Types = await db.CustomerTypes.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
    }

    private async Task<int> GetDefaultCountryIdAsync()
    {
        var country = await db.Countries.FirstOrDefaultAsync(x =>
            x.Code == "VN" ||
            x.Name == "Vietnam" ||
            x.Name == "Viet Nam");
        return country?.Id ?? (await db.Countries.OrderBy(x => x.Name).Select(x => x.Id).FirstOrDefaultAsync());
    }

    private void ValidateCreateRequiredFields(Customer item)
    {
        if (string.IsNullOrWhiteSpace(item.Phone))
            ModelState.AddModelError(nameof(item.Phone), "Vui lòng nhập số điện thoại.");

        if (item.CountryId <= 0)
            ModelState.AddModelError(nameof(item.CountryId), "Vui lòng chọn quốc gia.");

        if (!item.CustomerSourceId.HasValue || item.CustomerSourceId.Value <= 0)
            ModelState.AddModelError(nameof(item.CustomerSourceId), "Vui lòng chọn nguồn khách.");

        if (!item.CustomerTypeId.HasValue || item.CustomerTypeId.Value <= 0)
            ModelState.AddModelError(nameof(item.CustomerTypeId), "Vui lòng chọn loại khách.");
    }

}
