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
    public async Task<IActionResult> Index([FromQuery] string? name, [FromQuery] string? skills)
    {
        var isLoggedIn = User.Identity?.IsAuthenticated == true;

        var nameQuery = (name ?? string.Empty).Trim();
        var skillsQuery = (skills ?? string.Empty).Trim();

        var skillTokens = ParseSkillTokens(skillsQuery);

        // Base query: active users only.
        var q = _db.Users.AsNoTracking().Where(u => !u.IsDeactivated);

        // Privacy rule:
        // - anonymous: only public profiles
        // - logged in: include both
        if (!isLoggedIn)
        {
            q = q.Where(u => !u.IsProfilePrivate);
        }

        // Name filter: match first/last/full name prefix and contains.
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

        // Start selection with joins so we can filter on CV skills and compute project count.
        var rows = from u in q
                   join link in _db.ApplicationUserProfiles.AsNoTracking() on u.Id equals link.UserId into links
                   from link in links.DefaultIfEmpty()
                   join p in _db.Profiler.AsNoTracking() on link.ProfileId equals p.Id into profiles
                   from p in profiles.DefaultIfEmpty()
                   select new { u, p };

        // Skills filter: user matches if they have AT LEAST ONE of the tokens.
        // When both name + skills are provided, both must match (AND).
        if (skillTokens.Count > 0)
        {
            rows = rows.Where(x => x.p != null && x.p.SkillsCsv != null);

            // OR across tokens.
            var tokenRows = rows.Where(x => false);
            foreach (var token in skillTokens)
            {
                // Ensure token matching is robust with commas by using LIKE %token%.
                // (Skills are stored as CSV; we normalize tokens to lower and compare on lower-cased csv in memory later)
                var likeAnywhere = $"%{token}%";
                tokenRows = tokenRows.Concat(rows.Where(x => EF.Functions.Like(x.p!.SkillsCsv!, likeAnywhere)));
            }

            rows = tokenRows.Distinct();
        }

        // Limit results for UI safety.
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
                SelectedProjectsJson = x.p == null ? null : x.p.SelectedProjectsJson
            })
            .ToListAsync();

        var vm = new SearchCvVm
        {
            NameQuery = nameQuery,
            SkillsQuery = skillsQuery,
            ShowLoginTip = !isLoggedIn,
            Cvs = list.Select(x =>
            {
                var fullName = string.Join(' ', new[] { x.FirstName, x.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var skillsArr = ParseSkills(x.SkillsCsv);

                return new SearchCvVm.CvCardVm
                {
                    UserId = x.Id,
                    FullName = fullName,
                    Headline = string.IsNullOrWhiteSpace(x.Headline) ? "" : x.Headline,
                    City = x.City ?? string.Empty,
                    IsPrivate = x.IsProfilePrivate,
                    ProfileImagePath = !string.IsNullOrWhiteSpace(x.ProfileAvatar) ? x.ProfileAvatar : x.UserAvatar,
                    Skills = skillsArr,
                    ProjectCount = ParseSelectedProjectCount(x.SelectedProjectsJson)
                };
            }).ToList()
        };

        return View("SearchCV", vm);
    }

    private static List<string> ParseSkillTokens(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        // Allow comma or whitespace separated tokens, support things like ".NET".
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
