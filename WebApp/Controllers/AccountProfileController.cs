using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Identity;
using WebApp.Models;

namespace WebApp.Controllers;

[Authorize]
public class AccountProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountProfileController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
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

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
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

        return RedirectToAction("Index", "Home");
    }
}
