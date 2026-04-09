using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class CustomerServiceAddressesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int customerId, string? q, int page = 1, int pageSize = 10)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return NotFound();

        var query = db.CustomerServiceAddresses
            .Include(x => x.Address).ThenInclude(a => a!.Ward).ThenInclude(w => w!.Province)
            .Where(x => x.CustomerId == customerId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x => x.ContactName.Contains(q) || (x.ContactPhone ?? "").Contains(q) || (x.SiteName ?? "").Contains(q));
        }

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.IsDefault).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        ViewBag.Customer = customer;
        return View(new PagedResult<CustomerServiceAddress> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create(int customerId)
    {
        var customer = await db.Customers.FindAsync(customerId);
        if (customer is null) return NotFound();
        await LoadLookup(customerId);
        return View(new CustomerServiceAddress { CustomerId = customerId, ContactName = customer.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerServiceAddress item, string houseNumber, string street, int provinceId, int wardId)
    {
        var customer = await db.Customers.FindAsync(item.CustomerId);
        if (customer is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadLookup(item.CustomerId);
            return View(item);
        }

        var address = new Address { HouseNumber = houseNumber, Street = street, ProvinceId = provinceId, WardId = wardId };
        db.Addresses.Add(address);
        await db.SaveChangesAsync();

        item.AddressId = address.Id;
        db.CustomerServiceAddresses.Add(item);

        if (item.IsDefault)
        {
            var others = await db.CustomerServiceAddresses.Where(x => x.CustomerId == item.CustomerId).ToListAsync();
            foreach (var other in others) other.IsDefault = false;
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { customerId = item.CustomerId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.CustomerServiceAddresses.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        await LoadLookup(item.CustomerId);
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CustomerServiceAddress item, string houseNumber, string street, int provinceId, int wardId)
    {
        var old = await db.CustomerServiceAddresses.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();

        old.ContactName = item.ContactName;
        old.ContactPhone = item.ContactPhone;
        old.ContactEmail = item.ContactEmail;
        old.SiteName = item.SiteName;
        old.IsDefault = item.IsDefault;
        old.HasOwnInvoiceInfo = item.HasOwnInvoiceInfo;
        old.InvoiceCompanyName = item.InvoiceCompanyName;
        old.InvoiceTaxCode = item.InvoiceTaxCode;
        old.InvoiceAddress = item.InvoiceAddress;
        old.InvoiceEmail = item.InvoiceEmail;
        old.InvoicePhone = item.InvoicePhone;
        old.InvoiceReceiver = item.InvoiceReceiver;
        old.IsActive = item.IsActive;

        if (old.Address is not null)
        {
            old.Address.HouseNumber = houseNumber;
            old.Address.Street = street;
            old.Address.ProvinceId = provinceId;
            old.Address.WardId = wardId;
        }

        if (item.IsDefault)
        {
            var others = await db.CustomerServiceAddresses.Where(x => x.CustomerId == old.CustomerId && x.Id != old.Id).ToListAsync();
            foreach (var other in others) other.IsDefault = false;
        }

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { customerId = old.CustomerId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,DieuPhoi")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.CustomerServiceAddresses.FindAsync(id);
        if (item is null) return RedirectToAction("Index", "Customers");

        var customerId = item.CustomerId;
        db.CustomerServiceAddresses.Remove(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { customerId });
    }

    private async Task LoadLookup(int customerId)
    {
        ViewBag.Customer = await db.Customers.FindAsync(customerId);
        ViewBag.Provinces = await db.GhnProvinces.OrderBy(x => x.ProvinceName).ToListAsync();
        ViewBag.Wards = await db.GhnWards.OrderBy(x => x.WardName).ToListAsync();
    }
}
