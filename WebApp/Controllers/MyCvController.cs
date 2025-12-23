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
            // First time: after successful save, redirect back here so user sees the result.
            TempData["FirstCvEdit"] = "1";
            return RedirectToAction("Index", "EditCV");
        }

        var profile = await _db.Profiler
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == link.ProfileId);

        var visits = await _db.ProfilBesok
            .AsNoTracking()
            .CountAsync(v => v.ProfileId == link.ProfileId);

        // Resolve selected projects (stored as JSON array of ids in Profile.SelectedProjectsJson).
        var selectedProjectIds = ParseSelectedProjectIds(profile?.SelectedProjectsJson);

        var projects = new List<MyCvProjectCardVm>();
        if (selectedProjectIds.Length > 0)
        {
            // Keep order as in selectedProjectIds by sorting in-memory after fetch.
            var rows = await (from p in _db.Projekt.AsNoTracking()
                              join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                              from u in users.DefaultIfEmpty()
                              where selectedProjectIds.Contains(p.Id)
                              select new { p, u })
                .ToListAsync();

            var map = rows.ToDictionary(x => x.p.Id, x => x);
            foreach (var id in selectedProjectIds)
            {
                if (!map.TryGetValue(id, out var x)) continue;

                projects.Add(new MyCvProjectCardVm
                {
                    Id = x.p.Id,
                    Title = x.p.Titel,
                    ShortDescription = x.p.KortBeskrivning,
                    CreatedUtc = x.p.CreatedUtc,
                    ImagePath = x.p.ImagePath,
                    TechKeys = ParseCsv(x.p.TechStackKeysCsv),
                    CreatedBy = x.u == null
                        ? (x.p.CreatedByUserId ?? "")
                        : (string.IsNullOrWhiteSpace((x.u.FirstName + " " + x.u.LastName).Trim())
                            ? (x.u.Email ?? "")
                            : (x.u.FirstName + " " + x.u.LastName).Trim())
                });
            }
        }

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
            Skills = ParseSkills(profile?.SkillsCsv),

            Projects = projects
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

    private static int[] ParseSelectedProjectIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<int>();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<int[]>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))
                ?? Array.Empty<int>();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    private static string[] ParseCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] ParseSkills(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public sealed class MyCvProjectCardVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }
        public string? ImagePath { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public string[] TechKeys { get; init; } = Array.Empty<string>();
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

        // Selected projects to show on CV (read-only cards)
        public List<MyCvProjectCardVm> Projects { get; init; } = new();

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
