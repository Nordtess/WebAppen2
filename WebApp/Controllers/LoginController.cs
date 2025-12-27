using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

/// <summary>
/// Controller för inloggningssidan och ny-användarvyn.
/// </summary>
public class LoginController : Controller
{
    // Visar den namngivna vyn "Login" (används för att separera vy-namn från action).
    public IActionResult Index()
    {
        return View("Login");
    }

    // Visar registreringssidan för nya användare.
    public IActionResult NewUser()
    {
        return View();
    }
}
