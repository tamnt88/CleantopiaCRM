using CleantopiaCRM.Web.Data;
using CleantopiaCRM.Web.ViewModels.Menu;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Services;

public interface IMenuService
{
    Task<List<MenuNodeVm>> GetMenuAsync(string roleName);
}

public class MenuService(AppDbContext db) : IMenuService
{
    public async Task<List<MenuNodeVm>> GetMenuAsync(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName)) return [];
        List<int> allowedIds;
        List<Entities.MenuItem> allActive;
        try
        {
            allowedIds = await db.RoleMenus
                .Where(x => x.RoleName == roleName)
                .Select(x => x.MenuItemId)
                .ToListAsync();

            allActive = await db.MenuItems
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }
        catch
        {
            return GetFallbackMenu(roleName);
        }

        var allById = allActive.ToDictionary(x => x.Id);
        var effectiveIds = new HashSet<int>(allowedIds);
        foreach (var id in allowedIds)
        {
            var cursor = allById.GetValueOrDefault(id);
            while (cursor?.ParentId is int pid)
            {
                if (!effectiveIds.Add(pid)) break;
                cursor = allById.GetValueOrDefault(pid);
            }
        }

        var all = allActive.Where(x => effectiveIds.Contains(x.Id)).OrderBy(x => x.SortOrder).ToList();

        var lookup = all.ToDictionary(
            x => x.Id,
            x => new MenuNodeVm
            {
                Id = x.Id,
                Title = x.Title,
                Url = x.Url,
                IconCss = x.IconCss,
                SortOrder = x.SortOrder
            });

        var roots = new List<MenuNodeVm>();

        foreach (var item in all)
        {
            var node = lookup[item.Id];
            if (item.ParentId.HasValue && lookup.TryGetValue(item.ParentId.Value, out var parent))
                parent.Children.Add(node);
            else
                roots.Add(node);
        }

        foreach (var n in lookup.Values)
            n.Children = n.Children.OrderBy(x => x.SortOrder).ToList();

        return roots.OrderBy(x => x.SortOrder).ToList();
    }

    private static List<MenuNodeVm> GetFallbackMenu(string roleName)
    {
        var menu = new List<MenuNodeVm>
        {
            new() { Id = 1, Title = "Tổng quan", Url = "/Dashboard/Index", SortOrder = 10, IconCss = "fa-solid fa-chart-line" },
            new() { Id = 2, Title = "Khách hàng", Url = "/Customers", SortOrder = 20, IconCss = "fi-rr-users" },
            new() { Id = 3, Title = "Dịch vụ", Url = "/ServicePrices", SortOrder = 30, IconCss = "fa-solid fa-screwdriver-wrench" },
            new() { Id = 4, Title = "Lịch hẹn", Url = "/Appointments", SortOrder = 40, IconCss = "fa-regular fa-calendar-days" },
            new() { Id = 5, Title = "Báo giá", Url = "/Quotes", SortOrder = 50, IconCss = "fa-solid fa-file-invoice-dollar" },
            new() { Id = 6, Title = "Báo cáo", Url = "/Reports/Summary", SortOrder = 60, IconCss = "fi-rr-chart-line-up" }
        };

        if (roleName == "Admin")
            menu.Add(new MenuNodeVm { Id = 7, Title = "Hệ thống", Url = "/Users", SortOrder = 70, IconCss = "fa-solid fa-gear" });

        return menu;
    }
}
