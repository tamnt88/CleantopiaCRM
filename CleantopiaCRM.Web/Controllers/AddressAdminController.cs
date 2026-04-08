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
        await syncService.SyncAsync();
        TempData["Message"] = "Ðã d?ng b? GHN thành công.";
        return RedirectToAction("Index", "Dashboard");
    }
}
