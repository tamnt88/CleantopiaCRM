using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleantopiaCRM.Web.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    public IActionResult Index() => RedirectToAction("Index", "Dashboard");
}
