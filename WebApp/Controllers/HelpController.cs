using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

public sealed class HelpController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
