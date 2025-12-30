using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

/// <summary>
/// Controller för hjälp-sidan; exponerar enkel vy för hjälpinnehåll.
/// </summary>
[AllowAnonymous]
public sealed class HelpController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View("Index");
    }
}
