// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WebApp.Domain.Identity;

namespace WebApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            // Keep properties simple; we don't want client-side/field-level validation here.
            public string Email { get; set; }

            [DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var rawLogin = (Input?.Email ?? string.Empty).Trim();
            var password = Input?.Password ?? string.Empty;

            // Always use one generic message for "no match" cases.
            const string genericError = "Kunde inte hitta någon med det användarnamnet eller lösenordet. Vänligen försök igen.";

            if (string.IsNullOrWhiteSpace(rawLogin) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, genericError);
                return Page();
            }

            // Case-insensitive lookup via Identity normalizers.
            var normalizedUserName = _userManager.NormalizeName(rawLogin);
            var normalizedEmail = _userManager.NormalizeEmail(rawLogin);

            var user = await _userManager.FindByNameAsync(rawLogin)
                       ?? await _userManager.FindByEmailAsync(rawLogin)
                       ?? (normalizedUserName is null ? null : await _userManager.FindByNameAsync(normalizedUserName))
                       ?? (normalizedEmail is null ? null : await _userManager.FindByEmailAsync(normalizedEmail));

            if (user is not null && user.IsDeactivated)
            {
                ModelState.AddModelError(string.Empty, genericError);
                return Page();
            }

            // Use canonical username for sign-in.
            var userNameForSignIn = user?.UserName ?? rawLogin;

            var result = await _signInManager.PasswordSignInAsync(userNameForSignIn, password, isPersistent: Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return LocalRedirect(returnUrl);
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }

            ModelState.AddModelError(string.Empty, genericError);
            return Page();
        }
    }
}
