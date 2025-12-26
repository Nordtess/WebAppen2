using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure.Data;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public sealed class SearchCvController : Controller
{
    private readonly ApplicationDbContext _db;

    public SearchCvController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? name,
        [FromQuery] string? skills,
        [FromQuery] string? city,
        [FromQuery] string? mode)
    {
        var isLoggedIn = User.Identity?.IsAuthenticated == true;

        var nameQuery = (name ?? string.Empty).Trim();
        var skillsQuery = (skills ?? string.Empty).Trim();
        var cityQuery = (city ?? string.Empty).Trim();

        var useSimilarMode = string.Equals(mode, "similar", StringComparison.OrdinalIgnoreCase);

        // Similar-mode: ignore query string skills, use current user's skills instead.
        if (useSimilarMode && isLoggedIn)
        {
            var meId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(meId))
            {
                var myLink = await _db.ApplicationUserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == meId);
                if (myLink is not null)
                {
                    var myProfile = await _db.Profiler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == myLink.ProfileId);
                    skillsQuery = string.Join(' ', ParseSkills(myProfile?.SkillsCsv));
                }
            }
        }

        // Normal mode = AND filter (must match all provided skill tokens)
        // Similar mode = OR filter (match any of my skills)
        var requireAllSkills = !useSimilarMode;

        var skillTokens = ParseSkillTokens(skillsQuery);

        // Base query: active users only.
        var q = _db.Users.AsNoTracking().Where(u => !u.IsDeactivated);

        // Never show the viewer's own CV on SearchCv (MyCV exists for that)
        if (isLoggedIn)
        {
            var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(viewerId))
            {
                q = q.Where(u => u.Id != viewerId);
            }
        }

        // Privacy rule:
        if (!isLoggedIn)
        {
            q = q.Where(u => !u.IsProfilePrivate);
        }

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var likeAnywhere = $"%{nameQuery}%";

            q = q.Where(u =>
                (u.FirstName != null && EF.Functions.Like(u.FirstName, likeAnywhere)) ||
                (u.LastName != null && EF.Functions.Like(u.LastName, likeAnywhere)) ||
                (u.FirstNameNormalized != null && EF.Functions.Like(u.FirstNameNormalized, likeAnywhere.ToUpperInvariant())) ||
                (u.LastNameNormalized != null && EF.Functions.Like(u.LastNameNormalized, likeAnywhere.ToUpperInvariant())) ||
                EF.Functions.Like(((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(), likeAnywhere));
        }

        if (!string.IsNullOrWhiteSpace(cityQuery))
        {
            var likeAnywhere = $"%{cityQuery}%";
            q = q.Where(u => u.City != null && EF.Functions.Like(u.City, likeAnywhere));
        }

        // Join to profile table so we can filter on skills + read about/headline/selected projects.
        var rows = from u in q
                   join link in _db.ApplicationUserProfiles.AsNoTracking() on u.Id equals link.UserId into links
                   from link in links.DefaultIfEmpty()
                   join p in _db.Profiler.AsNoTracking() on link.ProfileId equals p.Id into profiles
                   from p in profiles.DefaultIfEmpty()
                   select new { u, link, p };

        // Filter by skills
        if (skillTokens.Count > 0)
        {
            rows = rows.Where(x => x.p != null && x.p.SkillsCsv != null);

            if (requireAllSkills)
            {
                // AND across tokens
                foreach (var token in skillTokens)
                {
                    var likeAnywhere = $"%{token}%";
                    rows = rows.Where(x => EF.Functions.Like(x.p!.SkillsCsv!, likeAnywhere));
                }
            }
            else
            {
                // OR across tokens
                var tokenRows = rows.Where(x => false);
                foreach (var token in skillTokens)
                {
                    var likeAnywhere = $"%{token}%";
                    tokenRows = tokenRows.Concat(rows.Where(x => EF.Functions.Like(x.p!.SkillsCsv!, likeAnywhere)));
                }

                rows = tokenRows.Distinct();
            }
        }

        const int max = 60;

        var list = await rows
            .OrderBy(x => x.u.FirstName)
            .ThenBy(x => x.u.LastName)
            .Take(max)
            .Select(x => new
            {
                x.u.Id,
                x.u.FirstName,
                x.u.LastName,
                x.u.City,
                x.u.IsProfilePrivate,
                UserAvatar = x.u.ProfileImagePath,

                Headline = x.p == null ? null : x.p.Headline,
                AboutMe = x.p == null ? null : x.p.AboutMe,
                ProfileAvatar = x.p == null ? null : x.p.ProfileImagePath,
                SkillsCsv = x.p == null ? null : x.p.SkillsCsv,
                SelectedProjectsJson = x.p == null ? null : x.p.SelectedProjectsJson,

                ProfileId = x.link == null ? (int?)null : x.link.ProfileId
            })
            .ToListAsync();

        // Load educations/experiences in batches for the ids we returned.
        var profileIds = list.Where(x => x.ProfileId != null).Select(x => x.ProfileId!.Value).Distinct().ToArray();

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

        var vm = new SearchCvVm
        {
            NameQuery = nameQuery,
            SkillsQuery = skillsQuery,
            CityQuery = cityQuery,
            Mode = useSimilarMode ? "similar" : "normal",
            ShowLoginTip = !isLoggedIn,
            Cvs = list.Select(x =>
            {
                var fullName = string.Join(' ', new[] { x.FirstName, x.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var skillsArr = ParseSkills(x.SkillsCsv);

                var pid = x.ProfileId;
                var edus = pid != null && eduByProfile.TryGetValue(pid.Value, out var eduList) ? eduList : new();
                var exps = pid != null && expByProfile.TryGetValue(pid.Value, out var expList) ? expList : new();

                return new SearchCvVm.CvCardVm
                {
                    UserId = x.Id,
                    FullName = fullName,
                    Headline = string.IsNullOrWhiteSpace(x.Headline) ? "" : x.Headline,
                    City = x.City ?? string.Empty,
                    IsPrivate = x.IsProfilePrivate,
                    ProfileImagePath = !string.IsNullOrWhiteSpace(x.ProfileAvatar) ? x.ProfileAvatar : x.UserAvatar,
                    AboutMe = x.AboutMe,
                    Skills = skillsArr,
                    ProjectCount = ParseSelectedProjectCount(x.SelectedProjectsJson),
                    Educations = edus.Take(2).Select(e => $"{e.Years} • {e.Program}").ToArray(),
                    Experiences = exps.Take(2).Select(e => $"{e.Years} • {e.Role} @ {e.Company}").ToArray()
                };
            }).ToList()
        };

        return View("SearchCV", vm);
    }

    private static List<string> ParseSkillTokens(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        var tokens = raw
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tokens;
    }

    private static string[] ParseSkills(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

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
