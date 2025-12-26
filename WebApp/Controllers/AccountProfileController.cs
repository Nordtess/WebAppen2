using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Helpers;
using WebApp.Domain.Identity;
using WebApp.Models;

namespace WebApp.Controllers;

/// <summary>
/// Hanterar visning och uppdatering av inloggad användares kontoprofil.
/// </summary>
[Authorize]
public class AccountProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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

        // Normaliserar namn för konsekvent visning och enklare sökning.
        user.FirstName = NameNormalizer.ToDisplayName(model.FirstName);
        user.LastName = NameNormalizer.ToDisplayName(model.LastName);
        user.FirstNameNormalized = NameNormalizer.ToNormalized(user.FirstName);
        user.LastNameNormalized = NameNormalizer.ToNormalized(user.LastName);

        user.PhoneNumberDisplay = model.PhoneNumberDisplay;
        user.City = model.City;
        user.PostalCode = model.PostalCode;

        // Persist onboarding completion.
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

        // Uppdaterar inloggningssessionen så att ändringar syns direkt.
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
}
