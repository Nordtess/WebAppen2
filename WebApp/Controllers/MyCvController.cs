using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Domain.Identity;

namespace WebApp.Controllers;

[Authorize]
public class MyCvController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public MyCvController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var model = new MyCvProfileViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            City = user.City ?? string.Empty,
            PostalCode = user.PostalCode ?? string.Empty,
            PhoneNumber = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty
        };

        return View("MyCV", model);
    }

    public sealed class MyCvProfileViewModel
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;

        public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        public string Location
        {
            get
            {
                var parts = new[] { PostalCode, City }.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                return parts.Length == 0 ? string.Empty : string.Join(" ", parts);
            }
        }

        public string Initials
        {
            get
            {
                var f = string.IsNullOrWhiteSpace(FirstName) ? "" : FirstName.Trim()[0].ToString();
                var l = string.IsNullOrWhiteSpace(LastName) ? "" : LastName.Trim()[0].ToString();
                return (f + l).ToUpperInvariant();
            }
        }
    }
}
