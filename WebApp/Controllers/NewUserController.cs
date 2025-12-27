using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

/// <summary>
/// Visar vyn för registrering av ny användare.
/// </summary>
public class NewUserController : Controller
{
    // Returnerar den namngivna vyn "NewUser".
    public IActionResult Index()
    {
        return View("NewUser");
    }
}
