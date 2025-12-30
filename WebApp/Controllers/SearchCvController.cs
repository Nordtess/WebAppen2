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
        [FromQuery] string? city,
        [FromQuery] string? mode,
        [FromQuery] string? skillIds,
        [FromQuery] string? source,
        [FromQuery] string? sourceUserId)
    {
        var isLoggedIn = User.Identity?.IsAuthenticated == true;

        var nameQuery = (name ?? string.Empty).Trim();
        var cityQuery = (city ?? string.Empty).Trim();
        var selectedSkillIds = ParseSkillIds(skillIds);
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
                       ProfileId = link == null ? (int?)null : link.ProfileId,
                       SelectedProjectsJson = p == null ? null : p.SelectedProjectsJson
                   };

        const int max = 60;
        var list = await rows
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Take(max)
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

        var compIdsByUser = userIds.Length == 0
            ? new Dictionary<string, int[]>()
            : await (from link in _db.ApplicationUserProfiles.AsNoTracking()
                     join uc in _db.AnvandarKompetenser.AsNoTracking() on link.UserId equals uc.UserId
                     where userIds.Contains(link.UserId)
                     select new { link.UserId, uc.CompetenceId })
                .ToListAsync()
                .ContinueWith(t => t.Result
                    .GroupBy(x => x.UserId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.CompetenceId).Distinct().ToArray()));

        int[] sourceSkillIds = Array.Empty<int>();
        if (useSimilarMode)
        {
            if (string.Equals(source, "me", StringComparison.OrdinalIgnoreCase))
            {
                var viewerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrWhiteSpace(viewerId))
                {
                    sourceSkillIds = await _db.AnvandarKompetenser.AsNoTracking()
                        .Where(x => x.UserId == viewerId)
                        .Select(x => x.CompetenceId)
                        .Distinct()
                        .ToArrayAsync();
                }
            }
            else if (!string.IsNullOrWhiteSpace(sourceUserId))
            {
                sourceSkillIds = await _db.AnvandarKompetenser.AsNoTracking()
                    .Where(x => x.UserId == sourceUserId)
                    .Select(x => x.CompetenceId)
                    .Distinct()
                    .ToArrayAsync();
            }
        }

        var cvs = new List<SearchCvVm.CvCardVm>();

        foreach (var x in list)
        {
            compIdsByUser.TryGetValue(x.Id, out var compIds);

            if (useSimilarMode)
            {
                if (sourceSkillIds.Length == 0) continue;
                if (compIds is null || compIds.Length == 0) continue;
                var matchCount = compIds.Intersect(sourceSkillIds).Distinct().Count();
                if (matchCount == 0) continue;

                var fullName = string.Join(' ', new[] { x.FirstName, x.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var pid = x.ProfileId;
                var edus = pid != null && eduByProfile.TryGetValue(pid.Value, out var eduList) ? eduList : new();
                var exps = pid != null && expByProfile.TryGetValue(pid.Value, out var expList) ? expList : new();
                var skillsArr = compByUser.TryGetValue(x.Id, out var arr) ? arr : Array.Empty<string>();
                var projectCount = ParseSelectedProjectIds(x.SelectedProjectsJson).Length;

                cvs.Add(new SearchCvVm.CvCardVm
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
                    Experiences = exps.Take(1).Select(e => $"{e.Years} • {e.Role} @ {e.Company}").ToArray(),
                    MatchCount = matchCount,
                    SourceTotal = sourceSkillIds.Length
                });
            }
            else
            {
                if (selectedSkillIds.Length > 0)
                {
                    if (compIds is null || compIds.Length == 0) continue;
                    var hasAll = selectedSkillIds.All(id => compIds.Contains(id));
                    if (!hasAll) continue;
                }

                var fullName = string.Join(' ', new[] { x.FirstName, x.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var pid = x.ProfileId;
                var edus = pid != null && eduByProfile.TryGetValue(pid.Value, out var eduList) ? eduList : new();
                var exps = pid != null && expByProfile.TryGetValue(pid.Value, out var expList) ? expList : new();
                var skillsArr = compByUser.TryGetValue(x.Id, out var arr) ? arr : Array.Empty<string>();
                if (selectedSkillIds.Length > 0 && skillsArr.Length == 0) continue;
                var projectCount = ParseSelectedProjectIds(x.SelectedProjectsJson).Length;

                cvs.Add(new SearchCvVm.CvCardVm
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
                });
            }
        }

        if (useSimilarMode && sourceSkillIds.Length > 0)
        {
            cvs = cvs
                .OrderByDescending(c => c.MatchCount ?? 0)
                .ThenBy(c => c.FullName)
                .ToList();
        }

        var competenceList = await _db.Kompetenskatalog.AsNoTracking()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.SortOrder)
            .Select(c => new SearchCvVm.CompetenceItemVm
            {
                Id = c.Id,
                Name = c.Name,
                Category = c.Category ?? string.Empty
            })
            .ToListAsync();

        var selectedSkillNames = competenceList
            .Where(c => selectedSkillIds.Contains(c.Id))
            .Select(c => c.Name)
            .ToArray();

        var vm = new SearchCvVm
        {
            NameQuery = nameQuery,
            CityQuery = cityQuery,
            Mode = useSimilarMode ? "similar" : "normal",
            ShowLoginTip = !isLoggedIn,
            SelectedSkillIds = selectedSkillIds,
            SelectedSkillNames = selectedSkillNames,
            SimilarSourceTotal = useSimilarMode ? sourceSkillIds.Length : 0,
            SimilarHint = useSimilarMode && sourceSkillIds.Length == 0 ? "Inga kompetenser att matcha på." : (useSimilarMode ? "Visar liknande personer baserat på kompetenser." : string.Empty),
            Source = source,
            SourceUserId = sourceUserId,
            Competences = competenceList,
            Cvs = cvs
        };

        return View("SearchCV", vm);
    }

    private static int[] ParseSkillIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<int>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
            .Where(n => n.HasValue && n.Value > 0)
            .Select(n => n!.Value)
            .Distinct()
            .ToArray();
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

