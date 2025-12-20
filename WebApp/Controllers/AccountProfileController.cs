using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Helpers;
using WebApp.Domain.Identity;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class AccountProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountProfileController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // GET: /AccountProfile
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View("AccountProfile", user);
    }

    // GET: /AccountProfile/Edit
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var vm = new AccountEditViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumberDisplay = user.PhoneNumberDisplay,
            City = user.City,
            PostalCode = user.PostalCode
        };

        return View("AccountEdit", vm);
    }

    // POST: /AccountProfile/Edit
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

        // Pretty display formatting
        user.FirstName = NameNormalizer.ToDisplayName(model.FirstName);
        user.LastName = NameNormalizer.ToDisplayName(model.LastName);

        // Normalized columns for filtering/search
        user.FirstNameNormalized = NameNormalizer.ToNormalized(user.FirstName);
        user.LastNameNormalized = NameNormalizer.ToNormalized(user.LastName);

        user.PhoneNumberDisplay = model.PhoneNumberDisplay;
        user.City = model.City;
        user.PostalCode = model.PostalCode;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            return View("AccountEdit", model);
        }

        // Ensure updated user values are reflected in the auth cookie.
        await _signInManager.RefreshSignInAsync(user);

        return RedirectToAction("Index", "AccountProfile");
    }

    // GET: /AccountProfile/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View("ChangePassword", new ChangePasswordViewModel());
    }

    // POST: /AccountProfile/ChangePassword
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

    // GET: /AccountProfile/Deactivate
    [HttpGet]
    public IActionResult Deactivate()
    {
        return View("Deactivate");
    }

    // POST: /AccountProfile/Deactivate
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
