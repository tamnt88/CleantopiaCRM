using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class ServiceUnitsController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Đơn vị tính";
        ViewData["BreadcrumbParent"] = "Bảng giá dịch vụ";
        ViewData["BreadcrumbParentUrl"] = "/ServicePrices";
        var items = await db.ServiceUnits.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        return View(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name, bool isActive = true)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Message"] = "Vui lòng nhập tên đơn vị.";
            return RedirectToAction(nameof(Index));
        }

        var exists = await db.ServiceUnits.AnyAsync(x => x.Name == name);
        if (exists)
        {
            TempData["Message"] = "Đơn vị đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        var max = await db.ServiceUnits.Select(x => (int?)x.SortOrder).MaxAsync() ?? 0;
        db.ServiceUnits.Add(new ServiceUnit { Name = name, SortOrder = max + 10, IsActive = isActive });
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã thêm đơn vị tính.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(int id, string name, bool isActive)
    {
        var item = await db.ServiceUnits.FindAsync(id);
        if (item is null) return RedirectToAction(nameof(Index));

        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Message"] = "Tên đơn vị không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var duplicate = await db.ServiceUnits.AnyAsync(x => x.Id != id && x.Name == name);
        if (duplicate)
        {
            TempData["Message"] = "Tên đơn vị đã tồn tại.";
            return RedirectToAction(nameof(Index));
        }

        item.Name = name;
        item.IsActive = isActive;
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật đơn vị tính.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var used = await db.ServicePricePolicies.AnyAsync(x => x.UnitId == id);
        if (used)
        {
            TempData["Message"] = "Đơn vị đang được sử dụng, không thể xóa.";
            return RedirectToAction(nameof(Index));
        }

        var item = await db.ServiceUnits.FindAsync(id);
        if (item is not null)
        {
            db.ServiceUnits.Remove(item);
            await db.SaveChangesAsync();
            TempData["Message"] = "Đã xóa đơn vị tính.";
        }

        return RedirectToAction(nameof(Index));
    }
}
