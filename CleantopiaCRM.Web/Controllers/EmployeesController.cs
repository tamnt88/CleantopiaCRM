using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi,GiamSat")]
public class EmployeesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? role, bool? isActive, int page = 1, int pageSize = 10)
    {
        var query = db.Employees.Include(x => x.Address).ThenInclude(a => a!.Ward).ThenInclude(w => w!.Province).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.FullName.Contains(q) || x.EmployeeCode.Contains(q) || x.Phone.Contains(q));
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(x => x.Role == role);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        return View(new PagedResult<Employee> { Items = items, Page = page, PageSize = pageSize, TotalItems = total, Query = q });
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookup();
        return View(new Employee());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Employee item, string houseNumber, string street, int provinceId, int wardId)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookup();
            return View(item);
        }

        var address = new Address { HouseNumber = houseNumber, Street = street, ProvinceId = provinceId, WardId = wardId };
        db.Addresses.Add(address);
        await db.SaveChangesAsync();
        item.AddressId = address.Id;
        db.Employees.Add(item);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.Employees.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        await LoadLookup();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, Employee item, string houseNumber, string street, int provinceId, int wardId)
    {
        var old = await db.Employees.Include(x => x.Address).FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();
        old.EmployeeCode = item.EmployeeCode;
        old.FullName = item.FullName;
        old.Phone = item.Phone;
        old.Email = item.Email;
        old.Role = item.Role;
        old.IsActive = item.IsActive;
        if (old.Address is not null)
        {
            old.Address.HouseNumber = houseNumber;
            old.Address.Street = street;
            old.Address.ProvinceId = provinceId;
            old.Address.WardId = wardId;
        }
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Employees.FindAsync(id);
        if (item is not null)
        {
            db.Employees.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookup()
    {
        ViewBag.Provinces = await db.GhnProvinces.OrderBy(x => x.ProvinceName).ToListAsync();
        ViewBag.Wards = await db.GhnWards.OrderBy(x => x.WardName).ToListAsync();
    }
}
