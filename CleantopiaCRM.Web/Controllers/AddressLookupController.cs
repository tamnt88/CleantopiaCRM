using CleantopiaCRM.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleantopiaCRM.Web.Controllers;

[Authorize]
[Route("api/address")]
public class AddressLookupController(AppDbContext db) : Controller
{
    [HttpGet("wards")]
    public async Task<IActionResult> Wards([FromQuery] int provinceId)
    {
        var wards = await db.GhnWards.Where(x => x.ProvinceId == provinceId)
            .OrderBy(x => x.WardName)
            .Select(x => new { x.Id, x.WardName })
            .ToListAsync();
        return Json(wards);
    }
}
