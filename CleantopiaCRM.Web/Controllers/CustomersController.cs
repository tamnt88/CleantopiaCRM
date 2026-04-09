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
        var defaultProvinceId = await GetDefaultProvinceIdAsync();
        await LoadLookup(defaultProvinceId);
        ViewBag.ProvinceId = defaultProvinceId;
        ViewBag.IsModal = IsModalRequest();
        return View(new Customer
        {
            CreatedAt = DateTime.Now,
            CountryId = defaultCountryId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Customer item,
        string houseNumber,
        string street,
        int provinceId,
        int wardId,
        string contactName,
        string? contactPhone,
        string? contactEmail,
        string? siteName)
    {
        ValidateCreateRequiredFields(item, houseNumber, street, provinceId, wardId, contactName, contactPhone);

        if (!ModelState.IsValid)
        {
            SetCreateFormInput(houseNumber, street, provinceId, wardId, siteName, contactName, contactPhone, contactEmail);
            await LoadLookup(provinceId);
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        ValidateBusinessFields(item);
        if (!ModelState.IsValid)
        {
            SetCreateFormInput(houseNumber, street, provinceId, wardId, siteName, contactName, contactPhone, contactEmail);
            await LoadLookup(provinceId);
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        var address = new Address { HouseNumber = houseNumber, Street = street, ProvinceId = provinceId, WardId = wardId };
        db.Addresses.Add(address);
        await db.SaveChangesAsync();

        item.CreatedAt = DateTime.Now;
        db.Customers.Add(item);
        await db.SaveChangesAsync();

        var serviceAddress = new CustomerServiceAddress
        {
            CustomerId = item.Id,
            AddressId = address.Id,
            ContactName = string.IsNullOrWhiteSpace(contactName) ? item.Name : contactName,
            ContactPhone = contactPhone,
            ContactEmail = contactEmail,
            SiteName = siteName,
            IsDefault = true,
            HasOwnInvoiceInfo = false,
            IsActive = true
        };

        db.CustomerServiceAddresses.Add(serviceAddress);
        await db.SaveChangesAsync();

        return ModalSuccessResult();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();

        var defaultAddress = await db.CustomerServiceAddresses
            .Include(x => x.Address)
            .Where(x => x.CustomerId == id)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync();

        ViewBag.DefaultAddress = defaultAddress;
        SetAddressFormInput(
            defaultAddress?.Address?.HouseNumber,
            defaultAddress?.Address?.Street,
            defaultAddress?.Address?.ProvinceId,
            defaultAddress?.Address?.WardId,
            defaultAddress?.SiteName,
            defaultAddress?.ContactName,
            defaultAddress?.ContactPhone,
            defaultAddress?.ContactEmail);
        await LoadLookup(defaultAddress?.Address?.ProvinceId);
        ViewBag.IsModal = IsModalRequest();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        int id,
        Customer item,
        int? defaultServiceAddressId,
        string houseNumber,
        string street,
        int provinceId,
        int wardId,
        string contactName,
        string? contactPhone,
        string? contactEmail,
        string? siteName)
    {
        var old = await db.Customers.FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();

        ValidateCreateRequiredFields(item, houseNumber, street, provinceId, wardId, contactName, contactPhone);
        if (!ModelState.IsValid)
        {
            ViewBag.DefaultAddress = await db.CustomerServiceAddresses.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == defaultServiceAddressId);
            SetAddressFormInput(houseNumber, street, provinceId, wardId, siteName, contactName, contactPhone, contactEmail);
            await LoadLookup(provinceId);
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        ValidateBusinessFields(item);
        if (!ModelState.IsValid)
        {
            ViewBag.DefaultAddress = await db.CustomerServiceAddresses.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == defaultServiceAddressId);
            SetAddressFormInput(houseNumber, street, provinceId, wardId, siteName, contactName, contactPhone, contactEmail);
            await LoadLookup(provinceId);
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        old.Name = item.Name;
        old.Phone = item.Phone;
        old.Email = item.Email;
        old.CountryId = item.CountryId;
        old.CustomerSourceId = item.CustomerSourceId;
        old.CustomerTypeId = item.CustomerTypeId;
        old.IsBusiness = item.IsBusiness;
        old.CompanyName = item.CompanyName;
        old.TaxCode = item.TaxCode;
        old.BillingAddress = item.BillingAddress;
        old.BillingEmail = item.BillingEmail;
        old.BillingPhone = item.BillingPhone;
        old.BillingReceiver = item.BillingReceiver;
        old.Notes = item.Notes;

        CustomerServiceAddress? serviceAddress = null;
        if (defaultServiceAddressId.HasValue)
        {
            serviceAddress = await db.CustomerServiceAddresses.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == defaultServiceAddressId.Value && x.CustomerId == id);
        }

        if (serviceAddress is null)
        {
            var newAddress = new Address { HouseNumber = houseNumber, Street = street, ProvinceId = provinceId, WardId = wardId };
            db.Addresses.Add(newAddress);
            await db.SaveChangesAsync();

            serviceAddress = new CustomerServiceAddress
            {
                CustomerId = id,
                AddressId = newAddress.Id,
                ContactName = string.IsNullOrWhiteSpace(contactName) ? old.Name : contactName,
                ContactPhone = contactPhone,
                ContactEmail = contactEmail,
                SiteName = siteName,
                IsDefault = true,
                IsActive = true
            };

            db.CustomerServiceAddresses.Add(serviceAddress);
        }
        else
        {
            if (serviceAddress.Address is not null)
            {
                serviceAddress.Address.HouseNumber = houseNumber;
                serviceAddress.Address.Street = street;
                serviceAddress.Address.ProvinceId = provinceId;
                serviceAddress.Address.WardId = wardId;
            }

            serviceAddress.ContactName = string.IsNullOrWhiteSpace(contactName) ? old.Name : contactName;
            serviceAddress.ContactPhone = contactPhone;
            serviceAddress.ContactEmail = contactEmail;
            serviceAddress.SiteName = siteName;
        }

        var allAddresses = await db.CustomerServiceAddresses.Where(x => x.CustomerId == id).ToListAsync();
        foreach (var addr in allAddresses) addr.IsDefault = addr.Id == serviceAddress.Id;

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

    private async Task LoadLookup(int? selectedProvinceId = null)
    {
        ViewBag.Countries = await db.Countries.OrderBy(x => x.Name).ToListAsync();
        var provinces = await db.GhnProvinces.OrderBy(x => x.ProvinceName).ToListAsync();
        ViewBag.Provinces = provinces;
        var provinceId = selectedProvinceId.GetValueOrDefault();
        if (provinceId <= 0)
        {
            provinceId = await GetDefaultProvinceIdAsync();
        }
        ViewBag.ProvinceId = provinceId;
        ViewBag.Wards = await db.GhnWards
            .Where(x => x.ProvinceId == provinceId)
            .OrderBy(x => x.WardName)
            .ToListAsync();
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

    private async Task<int> GetDefaultProvinceIdAsync()
    {
        var province = await db.GhnProvinces.FirstOrDefaultAsync(x =>
            x.ProvinceName.Contains("Hồ Chí Minh") ||
            x.ProvinceName.Contains("Ho Chi Minh"));
        return province?.Id ?? (await db.GhnProvinces.OrderBy(x => x.ProvinceName).Select(x => x.Id).FirstOrDefaultAsync());
    }

    private void ValidateCreateRequiredFields(Customer item, string houseNumber, string street, int provinceId, int wardId, string contactName, string? contactPhone)
    {
        if (string.IsNullOrWhiteSpace(item.Phone))
            ModelState.AddModelError(nameof(item.Phone), "Vui lòng nhập số điện thoại.");

        if (item.CountryId <= 0)
            ModelState.AddModelError(nameof(item.CountryId), "Vui lòng chọn quốc gia.");

        if (!item.CustomerSourceId.HasValue || item.CustomerSourceId.Value <= 0)
            ModelState.AddModelError(nameof(item.CustomerSourceId), "Vui lòng chọn nguồn khách.");

        if (!item.CustomerTypeId.HasValue || item.CustomerTypeId.Value <= 0)
            ModelState.AddModelError(nameof(item.CustomerTypeId), "Vui lòng chọn loại khách.");

        if (string.IsNullOrWhiteSpace(houseNumber))
            ModelState.AddModelError("houseNumber", "Vui lòng nhập số nhà.");

        if (string.IsNullOrWhiteSpace(street))
            ModelState.AddModelError("street", "Vui lòng nhập tên đường.");

        if (provinceId <= 0)
            ModelState.AddModelError("provinceId", "Vui lòng chọn tỉnh/thành.");

        if (wardId <= 0)
            ModelState.AddModelError("wardId", "Vui lòng chọn phường/xã.");

        if (string.IsNullOrWhiteSpace(contactName))
            ModelState.AddModelError("contactName", "Vui lòng nhập người liên hệ.");

        if (string.IsNullOrWhiteSpace(contactPhone))
            ModelState.AddModelError("contactPhone", "Vui lòng nhập số điện thoại liên hệ.");
    }

    private void SetCreateFormInput(string houseNumber, string street, int provinceId, int wardId, string? siteName, string contactName, string? contactPhone, string? contactEmail)
    {
        SetAddressFormInput(houseNumber, street, provinceId, wardId, siteName, contactName, contactPhone, contactEmail);
    }

    private void SetAddressFormInput(string? houseNumber, string? street, int? provinceId, int? wardId, string? siteName, string? contactName, string? contactPhone, string? contactEmail)
    {
        ViewBag.HouseNumber = houseNumber;
        ViewBag.Street = street;
        ViewBag.ProvinceId = provinceId;
        ViewBag.WardId = wardId;
        ViewBag.SiteName = siteName;
        ViewBag.ContactName = contactName;
        ViewBag.ContactPhone = contactPhone;
        ViewBag.ContactEmail = contactEmail;
    }

    private void ValidateBusinessFields(Customer item)
    {
        if (!item.IsBusiness) return;

        if (string.IsNullOrWhiteSpace(item.CompanyName))
            ModelState.AddModelError(nameof(item.CompanyName), "Khách doanh nghiệp phải có tên công ty.");

        if (string.IsNullOrWhiteSpace(item.TaxCode))
            ModelState.AddModelError(nameof(item.TaxCode), "Khách doanh nghiệp phải có mã số thuế.");

        if (string.IsNullOrWhiteSpace(item.BillingAddress))
            ModelState.AddModelError(nameof(item.BillingAddress), "Khách doanh nghiệp phải có địa chỉ xuất hóa đơn.");
    }
}
