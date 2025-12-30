using System.Security.Claims;
using System.Text.Json;
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

        var usersQuery = _db.Users.AsNoTracking().Where(u => !u.IsDeactivated);
        if (isLoggedIn)
        {
            var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(viewerId)) usersQuery = usersQuery.Where(u => u.Id != viewerId);
        }
        if (!isLoggedIn) usersQuery = usersQuery.Where(u => !u.IsProfilePrivate);

        if (!string.IsNullOrWhiteSpace(nameQuery))
        {
            var likeAnywhere = $"%{nameQuery}%";
            usersQuery = usersQuery.Where(u =>
                (u.FirstName != null && EF.Functions.Like(u.FirstName, likeAnywhere)) ||
                (u.LastName != null && EF.Functions.Like(u.LastName, likeAnywhere)) ||
                (u.FirstNameNormalized != null && EF.Functions.Like(u.FirstNameNormalized, likeAnywhere.ToUpperInvariant())) ||
                (u.LastNameNormalized != null && EF.Functions.Like(u.LastNameNormalized, likeAnywhere.ToUpperInvariant())) ||
                EF.Functions.Like(((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(), likeAnywhere));
        }

        if (!string.IsNullOrWhiteSpace(cityQuery))
        {
            var likeAnywhere = $"%{cityQuery}%";
            usersQuery = usersQuery.Where(u => u.City != null && EF.Functions.Like(u.City, likeAnywhere));
        }

        var rows = from u in usersQuery
                   join link in _db.ApplicationUserProfiles.AsNoTracking() on u.Id equals link.UserId into links
                   from link in links.DefaultIfEmpty()
                   join p in _db.Profiler.AsNoTracking() on link.ProfileId equals p.Id into profiles
                   from p in profiles.DefaultIfEmpty()
                   select new { u, link, p };

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
                ProfileId = x.link == null ? (int?)null : x.link.ProfileId,
                SelectedProjectsJson = x.p == null ? null : x.p.SelectedProjectsJson
            })
            .ToListAsync();

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

        var userIds = list.Select(x => x.Id).Distinct().ToArray();
        var compByUser = userIds.Length == 0
            ? new Dictionary<string, string[]>()
            : await (from link in _db.ApplicationUserProfiles.AsNoTracking()
                     join uc in _db.AnvandarKompetenser.AsNoTracking() on link.UserId equals uc.UserId
                     join c in _db.Kompetenskatalog.AsNoTracking() on uc.CompetenceId equals c.Id
                     where userIds.Contains(link.UserId)
                     orderby c.SortOrder
                     select new { link.UserId, c.Name })
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

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
                var pid = x.ProfileId;
                var edus = pid != null && eduByProfile.TryGetValue(pid.Value, out var eduList) ? eduList : new();
                var exps = pid != null && expByProfile.TryGetValue(pid.Value, out var expList) ? expList : new();
                var skillsArr = compByUser.TryGetValue(x.Id, out var arr) ? arr : Array.Empty<string>();
                var projectCount = ParseSelectedProjectIds(x.SelectedProjectsJson).Length;

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
                    ProjectCount = projectCount,
                    Educations = edus.Take(1).Select(e => $"{e.Years} • {e.Program}").ToArray(),
                    Experiences = exps.Take(1).Select(e => $"{e.Years} • {e.Role} @ {e.Company}").ToArray()
                };
            }).ToList()
        };

        return View("SearchCV", vm);
    }

    private static int[] ParseSelectedProjectIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<int>();

        try
        {
            var arr = JsonSerializer.Deserialize<int[]>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return arr?.Where(n => n > 0).Distinct().ToArray() ?? Array.Empty<int>();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }
}

