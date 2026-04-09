using CleantopiaCRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleantopiaCRM.Web.Controllers;

[Authorize(Roles = "Admin,DieuPhoi")]
public class AddressAdminController(GhnAddressSyncService syncService) : Controller
{
    [HttpPost]
    public async Task<IActionResult> SyncGhn()
    {
        try
        {
            await syncService.SyncAsync();
            TempData["Message"] = "Đã đồng bộ GHN thành công.";
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"Đồng bộ GHN thất bại: {ex.Message}";
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
