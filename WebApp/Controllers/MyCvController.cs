using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;

namespace WebApp.Controllers;

[Authorize]
public class MyCvController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;

    public MyCvController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var link = await _db.ApplicationUserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == user.Id);

        if (link is null)
        {
            return RedirectToAction("Index", "EditCV");
        }

        var profile = await _db.Profiler
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == link.ProfileId);

        var visits = await _db.ProfilBesok
            .AsNoTracking()
            .CountAsync(v => v.ProfileId == link.ProfileId);

        var model = new MyCvProfileViewModel
        {
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            City = user.City ?? string.Empty,
            PhoneNumber = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty,
            IsPrivate = user.IsProfilePrivate,
            VisitCount = visits,

            Headline = profile?.Headline,
            AboutMe = profile?.AboutMe,
            ProfileImagePath = profile?.ProfileImagePath ?? user.ProfileImagePath,
            Skills = ParseSkills(profile?.SkillsCsv)
        };

        return View("MyCV", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrivacy([FromForm] bool isPrivate)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        user.IsProfilePrivate = isPrivate;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest();
        }

        await _signInManager.RefreshSignInAsync(user);

        return Ok(new { isPrivate = user.IsProfilePrivate });
    }

    private static string[] ParseSkills(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public sealed class MyCvProfileViewModel
    {
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public bool IsPrivate { get; init; }
        public int VisitCount { get; init; }

        // CV-owned
        public string? Headline { get; init; }
        public string? AboutMe { get; init; }
        public string? ProfileImagePath { get; init; }
        public string[] Skills { get; init; } = Array.Empty<string>();

        public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

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
