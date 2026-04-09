using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.Entities;
using CleantopiaCRM.Web.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class ServicePricesController(AppDbContext db) : Controller
{
    private bool IsModalRequest()
    {
        if (string.Equals(Request.Query["modal"], "1", StringComparison.OrdinalIgnoreCase))
            return true;

        return Request.HasFormContentType
               && string.Equals(Request.Form["modal"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private IActionResult ModalSuccessResult(string fallbackAction)
    {
        if (!IsModalRequest())
            return RedirectToAction(fallbackAction);

        return Content(
            "<script>window.parent && window.parent.CRM && window.parent.CRM.closeServicePriceModal(true);</script>",
            "text/html");
    }

    public async Task<IActionResult> Index(string? q, string? category, bool? isActive, decimal? minPrice, decimal? maxPrice, int page = 1, int pageSize = 10)
    {
        var query = db.ServicePrices
            .Include(x => x.Policy)
                .ThenInclude(x => x!.Unit)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(x => x.ServiceName.Contains(q) || (x.VariantName ?? "").Contains(q));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);
        if (minPrice.HasValue) query = query.Where(x => x.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(x => x.Price <= maxPrice.Value);

        var items = await query.OrderBy(x => x.Category).ThenBy(x => x.DisplayOrder).ThenBy(x => x.ServiceName).ToListAsync();
        var total = items.Count;

        ViewBag.Category = category;
        ViewBag.IsActive = isActive;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();

        return View(new PagedResult<ServicePrice>
        {
            Items = items,
            Page = 1,
            PageSize = total == 0 ? 20 : total,
            TotalItems = total,
            Query = q
        });
    }

    public async Task<IActionResult> Categories()
    {
        var nodes = await db.ServiceCategories
            .AsNoTracking()
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var byParent = nodes.GroupBy(x => x.ParentId ?? 0).ToDictionary(g => g.Key, g => g.ToList());
        var parentMap = nodes.ToDictionary(x => x.Id, x => x.Name);

        int GetLevel(ServiceCategory node)
        {
            var lvl = 1;
            var p = node.ParentId;
            while (p.HasValue && parentMap.ContainsKey(p.Value))
            {
                lvl++;
                p = nodes.FirstOrDefault(x => x.Id == p.Value)?.ParentId;
                if (lvl > 10) break;
            }
            return lvl;
        }

        string GetPath(ServiceCategory node)
        {
            var names = new List<string> { node.Name };
            var p = node.ParentId;
            var guard = 0;
            while (p.HasValue && parentMap.TryGetValue(p.Value, out var pname) && guard < 10)
            {
                names.Insert(0, pname);
                p = nodes.FirstOrDefault(x => x.Id == p.Value)?.ParentId;
                guard++;
            }
            return string.Join(" > ", names);
        }

        var rows = nodes.Select(x => new ServiceCategoryVm
        {
            Id = x.Id,
            Name = x.Name,
            ParentId = x.ParentId,
            ParentName = x.ParentId.HasValue && parentMap.TryGetValue(x.ParentId.Value, out var pn) ? pn : null,
            Level = GetLevel(x),
            ChildCount = byParent.TryGetValue(x.Id, out var childs) ? childs.Count : 0,
            Path = GetPath(x),
            IsActive = x.IsActive
        }).ToList();

        ViewBag.ParentOptions = nodes
            .OrderBy(x => x.ParentId.HasValue ? 1 : 0)
            .ThenBy(x => x.Name)
            .ToList();

        return View(rows);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory(string name, int? parentId, bool isActive = true, string? returnUrl = null)
    {
        name = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Message"] = "Vui lòng nhập tên danh mục.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        if (parentId.HasValue && !await db.ServiceCategories.AnyAsync(x => x.Id == parentId.Value))
        {
            TempData["Message"] = "Danh mục cha không tồn tại.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var exists = await db.ServiceCategories.AnyAsync(x => x.ParentId == parentId && x.Name == name);
        if (exists)
        {
            TempData["Message"] = "Danh mục đã tồn tại ở cấp này.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var maxOrder = await db.ServiceCategories
            .Where(x => x.ParentId == parentId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? 0;

        db.ServiceCategories.Add(new ServiceCategory
        {
            Name = name,
            ParentId = parentId,
            IsActive = isActive,
            SortOrder = maxOrder + 1
        });

        await db.SaveChangesAsync();
        TempData["Message"] = "Đã tạo danh mục.";
        return RedirectToLocal(returnUrl, nameof(Categories));
    }

    [HttpPost]
    public async Task<IActionResult> RenameCategory(int id, string newName, int? parentId, bool isActive, string? returnUrl = null)
    {
        newName = (newName ?? string.Empty).Trim();

        if (id <= 0 || string.IsNullOrWhiteSpace(newName))
        {
            TempData["Message"] = "Thông tin danh mục không hợp lệ.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var node = await db.ServiceCategories.FindAsync(id);
        if (node is null)
        {
            TempData["Message"] = "Không tìm thấy danh mục.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        if (parentId == id)
        {
            TempData["Message"] = "Danh mục cha không hợp lệ.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        if (parentId.HasValue && !await db.ServiceCategories.AnyAsync(x => x.Id == parentId.Value))
        {
            TempData["Message"] = "Danh mục cha không tồn tại.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var duplicate = await db.ServiceCategories.AnyAsync(x => x.Id != id && x.ParentId == parentId && x.Name == newName);
        if (duplicate)
        {
            TempData["Message"] = "Tên danh mục đã tồn tại ở cấp này.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        node.Name = newName;
        node.ParentId = parentId;
        node.IsActive = isActive;
        await db.SaveChangesAsync();
        TempData["Message"] = "Đã cập nhật danh mục.";
        return RedirectToLocal(returnUrl, nameof(Categories));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, string? returnUrl = null)
    {
        if (id <= 0)
        {
            TempData["Message"] = "Danh mục không hợp lệ.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var node = await db.ServiceCategories.FindAsync(id);
        if (node is null)
        {
            TempData["Message"] = "Không tìm thấy danh mục.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        var hasChildren = await db.ServiceCategories.AnyAsync(x => x.ParentId == id);
        if (hasChildren)
        {
            TempData["Message"] = "Danh mục đang có cấp con, vui lòng xóa/cập nhật cấp con trước.";
            return RedirectToLocal(returnUrl, nameof(Categories));
        }

        db.ServiceCategories.Remove(node);
        await db.SaveChangesAsync();

        TempData["Message"] = "Đã xóa danh mục.";
        return RedirectToLocal(returnUrl, nameof(Categories));
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.IsModal = IsModalRequest();
        return View(new ServicePrice());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServicePrice item)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }
        item.VariantName = null;
        item.Description = null;
        item.IsActive = true;
        item.DisplayOrder = 0;
        item.Unit = await db.ServiceUnits.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => x.Name).FirstOrDefaultAsync() ?? "Gói";
        db.ServicePrices.Add(item);
        await db.SaveChangesAsync();

        var unitId = await db.ServiceUnits.Where(x => x.Name == item.Unit).Select(x => x.Id).FirstOrDefaultAsync();
        db.ServicePricePolicies.Add(new ServicePricePolicy
        {
            ServicePriceId = item.Id,
            UnitId = unitId > 0 ? unitId : await db.ServiceUnits.Where(x => x.IsActive).OrderBy(x => x.SortOrder).Select(x => x.Id).FirstOrDefaultAsync(),
            RecareCycleDays = 180
        });
        await db.SaveChangesAsync();
        return ModalSuccessResult(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await db.ServicePrices.Include(x => x.Policy).FirstOrDefaultAsync(x => x.Id == id);
        ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
        ViewBag.IsModal = IsModalRequest();
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ServicePrice item)
    {
        var old = await db.ServicePrices.Include(x => x.Policy).FirstOrDefaultAsync(x => x.Id == id);
        if (old is null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await db.ServicePrices.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync();
            ViewBag.IsModal = IsModalRequest();
            return View(item);
        }

        old.ServiceName = item.ServiceName;
        old.Category = item.Category;
        old.Price = item.Price;

        await db.SaveChangesAsync();
        return ModalSuccessResult(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ServicePrices.FindAsync(id);
        if (item is not null)
        {
            db.ServicePrices.Remove(item);
            await db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToLocal(string? returnUrl, string fallbackAction)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(fallbackAction);
    }
}

public class ServiceCategoryVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int Level { get; set; }
    public int ChildCount { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
