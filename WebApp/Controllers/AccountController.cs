using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Identity;

namespace WebApp.Controllers;

/// <summary>
/// Enkel controller som exponerar små Identity-relaterade endpoints användade av UI/ navigation.
/// Inloggning/registrering hanteras av Razor Pages under /Identity/Account/.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(SignInManager<ApplicationUser> signInManager)
    {
        _signInManager = signInManager;
    }

    /// <summary>
    /// POST: /Account/Logout
    /// Loggar ut användaren och omdirigerar till startsidan.
    /// </summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string? returnUrl = null)
    {
        // Loggar ut Identity (raderar auth-cookie och kopplade sessioner enligt SignInManager-implementationen).
        await _signInManager.SignOutAsync();

        // Försök redirecta till angiven lokal returnUrl; om den saknas använd Home/Index som fallback.
        // LocalRedirect används för att undvika öppna redirect-vulnerabiliteter.
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
    }
}
