using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Helpers;
using WebApp.Domain.Identity;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

/// <summary>
/// Controller för visning och uppdatering av den inloggade användarens kontoprofil.
/// </summary>
[Authorize]
public class AccountProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AccountDeletionService _deletionService;

    public AccountProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AccountDeletionService deletionService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _deletionService = deletionService;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View("AccountProfile", user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!user.HasCompletedAccountProfile)
        {
            ViewData["ToastTitle"] = "Välkommen!";
            ViewData["ToastMessage"] = "Komplettera ditt konto med dina personliga uppgifter så att du blir synlig för andra.";
        }

        var viewModel = new AccountEditViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumberDisplay = user.PhoneNumberDisplay ?? string.Empty,
            City = user.City,
            PostalCode = user.PostalCode
        };

        return View("AccountEdit", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AccountEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("AccountEdit", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        // Normalisera namn för konsekvent presentation och sökning.
        user.FirstName = NameNormalizer.ToDisplayName(model.FirstName);
        user.LastName = NameNormalizer.ToDisplayName(model.LastName);
        user.FirstNameNormalized = NameNormalizer.ToNormalized(user.FirstName);
        user.LastNameNormalized = NameNormalizer.ToNormalized(user.LastName);

        user.PhoneNumberDisplay = model.PhoneNumberDisplay;
        user.City = NameNormalizer.ToDisplayName(model.City);
        user.PostalCode = model.PostalCode;

        // Persist onboarding-completion för att visa relevanta meddelanden/flows.
        var wasCompleted = user.HasCompletedAccountProfile;
        user.HasCompletedAccountProfile = true;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View("AccountEdit", model);
        }

        // Uppdatera inloggningssessionen så att nya claims/profilfält reflekteras omedelbart.
        await _signInManager.RefreshSignInAsync(user);

        if (!wasCompleted)
        {
            TempData["ToastTitle"] = "Klart!";
            TempData["ToastMessage"] = "Dina uppgifter är sparade.";
        }

        return RedirectToAction("Index", "AccountProfile");
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View("ChangePassword", new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("ChangePassword", model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View("ChangePassword", model);
        }

        await _signInManager.RefreshSignInAsync(user);
        return RedirectToAction("Index", "AccountProfile");
    }

    [HttpGet]
    public IActionResult Deactivate()
    {
        return View("Deactivate");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateConfirmed()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.IsDeactivated = true;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View("Deactivate");
        }

        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Delete()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        return View("Delete", user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount([FromForm] string ConfirmText, [FromForm] string Password)
    {
        // Bekräftelsetext måste matcha exakt (case-insensitive) för att förhindra oavsiktlig radering.
        if (!string.Equals(ConfirmText?.Trim(), "RADERA", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Du måste skriva RADERA för att bekräfta.";
            return RedirectToAction("Index", "AccountProfile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        // Verifiera lösenord innan destruktiva operationer utförs.
        var ok = await _userManager.CheckPasswordAsync(user, Password ?? "");
        if (!ok)
        {
            TempData["Error"] = "Fel lösenord.";
            return RedirectToAction("Index", "AccountProfile");
        }

        // Först ta bort ägda domändata via tjänsten; om detta misslyckas avbryt och informera användaren.
        var result = await _deletionService.DeleteUserAndOwnedDataAsync(user.Id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage ?? "Kunde inte radera kontot.";
            return RedirectToAction("Index", "AccountProfile");
        }

        // Slutligen ta bort Identity-användaren.
        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
        {
            TempData["Error"] = "Kunde inte radera identity-kontot.";
            return RedirectToAction("Index", "AccountProfile");
        }

        await _signInManager.SignOutAsync();
        TempData["Success"] = "Ditt konto raderades permanent.";
        return RedirectToAction("Index", "Home");
    }
}
