using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;

namespace WebApp.Controllers;

/// <summary>
/// Hanterar vy för användarens egen CV-sida och relaterade åtgärder (self-heal för äldre konton, privacy, valda projekt).
/// </summary>
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

        // Återställ konsistens för äldre konton: om användaren har flaggat HasCreatedCv men länk saknas,
        // försök hitta eller skapa profil och återställ koppling.
        if (link is null && user.HasCreatedCv)
        {
            var existingProfile = await _db.Profiler
                .FirstOrDefaultAsync(p => p.OwnerUserId == user.Id);

            if (existingProfile is null)
            {
                var now = DateTimeOffset.UtcNow;
                existingProfile = new WebApp.Domain.Entities.Profile
                {
                    OwnerUserId = user.Id,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    IsPublic = true
                };

                _db.Profiler.Add(existingProfile);
                await _db.SaveChangesAsync();
            }

            var newLink = new WebApp.Domain.Entities.ApplicationUserProfile
            {
                UserId = user.Id,
                ProfileId = existingProfile.Id
            };

            _db.ApplicationUserProfiles.Add(newLink);
            await _db.SaveChangesAsync();

            // Ladda om länk som no-tracking för vidare användning i denna action.
            link = await _db.ApplicationUserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);
        }

        if (link is null)
        {
            // Markera att vi är i "första CV-edit"-flödet så EditCV kan återvända hit efter save.
            TempData["FirstCvEdit"] = "1";

            if (!user.HasCreatedCv)
            {
                return RedirectToAction("Index", "EditCV", new
                {
                    toastTitle = "Skapa ditt första CV",
                    toastMessage = "Du har inget CV ännu. Fyll i CV-sidan och klicka ‘Spara ändringar’ så visas det i ‘Mitt CV’."
                });
            }

            return RedirectToAction("Index", "EditCV");
        }

        var profile = await _db.Profiler
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == link.ProfileId);

        var visits = await _db.ProfilBesok
            .AsNoTracking()
            .CountAsync(v => v.ProfileId == link.ProfileId);

        // Utbildningar och erfarenheter hämtas från separata tabeller och ordnas med SortOrder.
        var educations = await _db.Utbildningar
            .AsNoTracking()
            .Where(x => x.ProfileId == link.ProfileId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new MyCvEducationItemVm
            {
                School = x.School,
                Program = x.Program,
                Years = x.Years,
                Note = x.Note
            })
            .ToListAsync();

        var experiences = await _db.Erfarenheter
            .AsNoTracking()
            .Where(x => x.ProfileId == link.ProfileId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new MyCvExperienceItemVm
            {
                Company = x.Company,
                Role = x.Role,
                Years = x.Years,
                Description = x.Description
            })
            .ToListAsync();

        // Valda projekt lagras som JSON-array med projekt-IDs; parsas och hämtas här.
        var selectedProjectIds = ParseSelectedProjectIds(profile?.SelectedProjectsJson);

        var projects = new List<MyCvProjectCardVm>();
        if (selectedProjectIds.Length > 0)
        {
            // Hämta projektrader och bygg ordnade kort enligt ordningen i selectedProjectIds.
            var rows = await (from p in _db.Projekt.AsNoTracking()
                              join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                              from u in users.DefaultIfEmpty()
                              where selectedProjectIds.Contains(p.Id)
                              select new { p, u })
                .ToListAsync();

            var map = rows.ToDictionary(x => x.p.Id, x => x);
            foreach (var id in selectedProjectIds.Take(4))
            {
                if (!map.TryGetValue(id, out var x)) continue;

                projects.Add(new MyCvProjectCardVm
                {
                    Id = x.p.Id,
                    Title = x.p.Titel,
                    ShortDescription = x.p.KortBeskrivning,
                    Description = x.p.Beskrivning,
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
            Skills = NormalizeSkillsForDisplay(ParseSkills(profile?.SkillsCsv)),

            Educations = educations,
            Experiences = experiences,
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

        // Uppdatera inloggningen så ändringen syns direkt i claims/session.
        await _signInManager.RefreshSignInAsync(user);

        return Ok(new { isPrivate = user.IsProfilePrivate });
    }

    private static int[] ParseSelectedProjectIds(string? json)
    {
        // Parsar JSON-array av ints; fel -> tom array.
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

    private static string[] NormalizeSkillsForDisplay(string[] skills)
    {
        if (skills.Length == 0) return skills;

        static string Display(string s)
        {
            var t = (s ?? string.Empty).Trim();
            if (t.Length == 0) return "";

            var low = t.ToLowerInvariant();
            return low switch
            {
                "c#" => "C#",
                "f#" => "F#",
                "sql" => "SQL",
                "html" => "HTML",
                "css" => "CSS",
                "js" or "javascript" => "JavaScript",
                "ts" or "typescript" => "TypeScript",
                ".net" or "dotnet" => ".NET",
                "asp.net" or "aspnet" => "ASP.NET",
                "mvc" => "MVC",
                "api" => "API",
                "mongodb" or "mongo db" or "mongo-db" => "MongoDB",
                "aws" => "AWS",
                "azure" => "Azure",
                _ => (t.Length <= 4 && t.All(char.IsLetter)) ? t.ToUpperInvariant() : char.ToUpper(t[0]) + t[1..]
            };
        }

        // Normalisera och ta bort dubbletter (skiftlägesokänsligt).
        return skills
            .Select(Display)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public sealed class MyCvEducationItemVm
    {
        public string School { get; init; } = string.Empty;
        public string Program { get; init; } = string.Empty;
        public string Years { get; init; } = string.Empty;
        public string? Note { get; init; }
    }

    public sealed class MyCvExperienceItemVm
    {
        public string Company { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Years { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    public sealed class MyCvProjectCardVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public string? Description { get; init; }
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

        public List<MyCvEducationItemVm> Educations { get; init; } = new();
        public List<MyCvExperienceItemVm> Experiences { get; init; } = new();

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
