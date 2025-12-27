using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure.Data;
using WebApp.Models;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Bygger modell för startsidan: senaste projektet och ett antal publika CV-kort.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var row = await (from p in _db.Projekt.AsNoTracking()
                         join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                         from u in users.DefaultIfEmpty()
                         orderby p.CreatedUtc descending
                         select new HomeIndexVm.LatestProjectVm
                         {
                             Id = p.Id,
                             Title = p.Titel,
                             ShortDescription = p.KortBeskrivning,
                             Description = p.Beskrivning,
                             CreatedUtc = p.CreatedUtc,
                             ImagePath = p.ImagePath,
                             TechKeysCsv = p.TechStackKeysCsv,
                             CreatedByName = u == null ? null : ((u.FirstName + " " + u.LastName).Trim()),
                             CreatedByEmail = u == null ? null : u.Email
                         })
            .FirstOrDefaultAsync();

        const int maxCvCards = 3;

        var latestUsers = await (from u in _db.Users.AsNoTracking()
                                 where !u.IsDeactivated && !u.IsProfilePrivate
                                 orderby u.CreatedUtc descending
                                 join link in _db.ApplicationUserProfiles.AsNoTracking() on u.Id equals link.UserId into links
                                 from link in links.DefaultIfEmpty()
                                 join p in _db.Profiler.AsNoTracking() on link.ProfileId equals p.Id into profiles
                                 from p in profiles.DefaultIfEmpty()
                                 select new
                                 {
                                     u.Id,
                                     u.FirstName,
                                     u.LastName,
                                     u.City,
                                     u.IsProfilePrivate,
                                     UserAvatar = u.ProfileImagePath,
                                     Headline = p == null ? null : p.Headline,
                                     AboutMe = p == null ? null : p.AboutMe,
                                     ProfileAvatar = p == null ? null : p.ProfileImagePath,
                                     SkillsCsv = p == null ? null : p.SkillsCsv,
                                     SelectedProjectsJson = p == null ? null : p.SelectedProjectsJson,
                                     ProfileId = link == null ? (int?)null : link.ProfileId
                                 })
            .Take(maxCvCards)
            .ToListAsync();

        var profileIds = latestUsers.Where(x => x.ProfileId != null).Select(x => x.ProfileId!.Value).Distinct().ToArray();

        // Hämta utbildningar per profil och gruppera i minnet för snabb access när vy-modellen byggs.
        var eduByProfile = profileIds.Length == 0
            ? new Dictionary<int, List<(string School, string Program, string Years)>>()
            : await _db.Utbildningar.AsNoTracking()
                .Where(e => profileIds.Contains(e.ProfileId))
                .OrderBy(e => e.SortOrder)
                .Select(e => new { e.ProfileId, e.School, e.Program, e.Years })
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .GroupBy(x => x.ProfileId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => (x.School, x.Program, x.Years)).ToList()));

        // Samma som ovan, men för arbetslivserfarenheter.
        var expByProfile = profileIds.Length == 0
            ? new Dictionary<int, List<(string Company, string Role, string Years)>>()
            : await _db.Erfarenheter.AsNoTracking()
                .Where(e => profileIds.Contains(e.ProfileId))
                .OrderBy(e => e.SortOrder)
                .Select(e => new { e.ProfileId, e.Company, e.Role, e.Years })
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .GroupBy(x => x.ProfileId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => (x.Company, x.Role, x.Years)).ToList()));

        var vm = new HomeIndexVm
        {
            LatestProject = row,
            LatestPublicCvs = latestUsers.Select(x =>
            {
                var fullName = string.Join(' ', new[] { x.FirstName, x.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

                var pid = x.ProfileId;
                var edus = pid != null && eduByProfile.TryGetValue(pid.Value, out var eduList) ? eduList : new();
                var exps = pid != null && expByProfile.TryGetValue(pid.Value, out var expList) ? expList : new();

                return new HomeIndexVm.CvCardVm
                {
                    UserId = x.Id,
                    FullName = fullName,
                    Headline = string.IsNullOrWhiteSpace(x.Headline) ? "" : x.Headline,
                    City = x.City ?? string.Empty,
                    IsPrivate = x.IsProfilePrivate,
                    ProfileImagePath = !string.IsNullOrWhiteSpace(x.ProfileAvatar) ? x.ProfileAvatar : x.UserAvatar,
                    AboutMe = x.AboutMe,
                    Skills = ParseSkills(x.SkillsCsv),
                    ProjectCount = ParseSelectedProjectCount(x.SelectedProjectsJson),
                    Educations = edus.Take(1).Select(e => $"{e.Years} • {e.Program}").ToArray(),
                    Experiences = exps.Take(1).Select(e => $"{e.Years} • {e.Role} @ {e.Company}").ToArray()
                };
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // Parse skills CSV från profil till array av tokens.
    private static string[] ParseSkills(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // Räkna ut hur många valda projekt som finns i JSON (felfall -> 0).
    private static int ParseSelectedProjectCount(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return 0;

        try
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<int[]>(json, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
            return ids?.Length ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}

public sealed class HomeIndexVm
{
    public LatestProjectVm? LatestProject { get; init; }

    public List<CvCardVm> LatestPublicCvs { get; init; } = new();

    public sealed class LatestProjectVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public string? Description { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }
        public string? ImagePath { get; init; }
        public string? TechKeysCsv { get; init; }
        public string? CreatedByName { get; init; }
        public string? CreatedByEmail { get; init; }
    }

    public sealed class CvCardVm
    {
        public string UserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? Headline { get; init; }
        public string City { get; init; } = string.Empty;
        public bool IsPrivate { get; init; }
        public string? ProfileImagePath { get; init; }
        public string? AboutMe { get; init; }
        public string[] Skills { get; init; } = Array.Empty<string>();
        public int ProjectCount { get; init; }
        public string[] Educations { get; init; } = Array.Empty<string>();
        public string[] Experiences { get; init; } = Array.Empty<string>();
    }
}
