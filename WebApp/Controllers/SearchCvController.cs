using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;
using WebApp.ViewModels;

namespace WebApp.Controllers;

public class SearchCvController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SearchCvController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] string? name, [FromQuery] string? city, [FromQuery] string? mode = "normal", [FromQuery] string? skillIds = null, [FromQuery] string? source = null, [FromQuery] string? sourceUserId = null, [FromQuery] string? sort = null)
    {
        var isAuthenticated = User?.Identity?.IsAuthenticated == true;
        var currentUserId = isAuthenticated && User != null ? _userManager.GetUserId(User) : null;

        var selectedSkillIds = ParseSkillIds(skillIds);
        var selectedSkillSet = selectedSkillIds.ToHashSet();

        var isSimilarMode = string.Equals(mode, "similar", StringComparison.OrdinalIgnoreCase);
        var sourceUser = !string.IsNullOrWhiteSpace(sourceUserId)
            ? sourceUserId
            : (string.Equals(source, "me", StringComparison.OrdinalIgnoreCase) ? currentUserId : null);

        // Hämta katalog med Topplista-flagga
        var competences = await _db.Kompetenskatalog
            .AsNoTracking()
            .OrderByDescending(c => c.IsTopList)
            .ThenBy(c => c.Category)
            .ThenBy(c => c.SortOrder)
            .Select(c => new SearchCvVm.CompetenceItemVm
            {
                Id = c.Id,
                Name = c.Name,
                Category = c.Category,
                IsTopList = c.IsTopList,
                SortOrder = c.SortOrder
            })
            .ToListAsync();

        var nameById = competences.ToDictionary(c => c.Id, c => c.Name);
        var selectedSkillNames = selectedSkillIds
            .Select(id => nameById.TryGetValue(id, out var n) ? n : string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        int[] sourceSkillIds = Array.Empty<int>();
        if (isSimilarMode && !string.IsNullOrWhiteSpace(sourceUser))
        {
            sourceSkillIds = await _db.AnvandarKompetenser
                .AsNoTracking()
                .Where(x => x.UserId == sourceUser)
                .Select(x => x.CompetenceId)
                .Distinct()
                .ToArrayAsync();
        }

        if (isSimilarMode && sourceSkillIds.Length == 0)
        {
            // Saknar käll-kompetenser: fall tillbaka till normal läge
            isSimilarMode = false;
            mode = "normal";
        }

        var nameTerm = (name ?? string.Empty).Trim().ToLowerInvariant();
        var cityTerm = (city ?? string.Empty).Trim().ToLowerInvariant();

        var baseQuery = from link in _db.ApplicationUserProfiles.AsNoTracking()
                        join profile in _db.Profiler.AsNoTracking() on link.ProfileId equals profile.Id
                        join user in _db.Users.AsNoTracking() on link.UserId equals user.Id
                        select new
                        {
                            link.UserId,
                            profile,
                            user
                        };

        baseQuery = baseQuery.Where(x => !x.user.IsDeactivated);

        if (!isAuthenticated)
        {
            baseQuery = baseQuery.Where(x => x.profile.IsPublic && !x.user.IsProfilePrivate);
        }

        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            baseQuery = baseQuery.Where(x => x.UserId != currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(sourceUser))
        {
            baseQuery = baseQuery.Where(x => x.UserId != sourceUser);
        }

        if (!string.IsNullOrWhiteSpace(nameTerm))
        {
            baseQuery = baseQuery.Where(x => (x.user.FirstName + " " + x.user.LastName).ToLower().Contains(nameTerm));
        }

        if (!string.IsNullOrWhiteSpace(cityTerm))
        {
            baseQuery = baseQuery.Where(x => (x.user.City ?? string.Empty).ToLower().Contains(cityTerm));
        }

        var list = await baseQuery.ToListAsync();
        var userIds = list.Select(x => x.UserId).ToArray();
        var profileIds = list.Select(x => x.profile.Id).ToArray();

        var compByUser = await _db.AnvandarKompetenser
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(x => x.CompetenceId).Distinct().ToArray());

        var eduLookup = await _db.Utbildningar
            .AsNoTracking()
            .Where(e => profileIds.Contains(e.ProfileId))
            .OrderBy(e => e.SortOrder)
            .GroupBy(e => e.ProfileId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(e => $"{e.School} • {e.Program} • {e.Years}").ToArray()
            );

        var expLookup = await _db.Erfarenheter
            .AsNoTracking()
            .Where(e => profileIds.Contains(e.ProfileId))
            .OrderBy(e => e.SortOrder)
            .GroupBy(e => e.ProfileId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(e => $"{e.Company} • {e.Role} • {e.Years}").ToArray()
            );

        var cvs = new List<SearchCvVm.CvCardVm>();

        foreach (var row in list)
        {
            compByUser.TryGetValue(row.UserId, out var compsForUser);
            compsForUser ??= Array.Empty<int>();
            var compSet = compsForUser.ToHashSet();

            if (selectedSkillSet.Count > 0 && !selectedSkillSet.IsSubsetOf(compSet))
            {
                continue;
            }

            int? matchCount = null;
            int? sourceTotal = null;
            if (isSimilarMode)
            {
                var intersect = compSet.Intersect(sourceSkillIds);
                matchCount = intersect.Count();
                sourceTotal = sourceSkillIds.Length;
                if (matchCount == 0)
                {
                    continue;
                }
            }

            var skillNames = compsForUser
                .Select(id => nameById.TryGetValue(id, out var n) ? n : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            eduLookup.TryGetValue(row.profile.Id, out var edu);
            expLookup.TryGetValue(row.profile.Id, out var exp);

            cvs.Add(new SearchCvVm.CvCardVm
            {
                UserId = row.UserId,
                FullName = string.Join(' ', new[] { row.user.FirstName, row.user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Headline = row.profile.Headline,
                City = row.user.City ?? string.Empty,
                IsPrivate = !row.profile.IsPublic,
                ProfileImagePath = string.IsNullOrWhiteSpace(row.profile.ProfileImagePath) ? row.user.ProfileImagePath : row.profile.ProfileImagePath,
                AboutMe = row.profile.AboutMe,
                Skills = skillNames,
                Educations = edu ?? Array.Empty<string>(),
                Experiences = exp ?? Array.Empty<string>(),
                ProjectCount = ParseProjectIds(row.profile.SelectedProjectsJson).Length,
                MatchCount = matchCount,
                SourceTotal = sourceTotal,
                CreatedUtc = row.profile.CreatedUtc
            });
        }

        var sortKeyRaw = sort ?? string.Empty;
        var sortKey = string.IsNullOrWhiteSpace(sortKeyRaw)
            ? (isSimilarMode ? "match" : "new")
            : sortKeyRaw.Trim().ToLowerInvariant();

        if (isSimilarMode)
        {
            sortKey = "match";
        }

        var validSort = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "match", "new", "old", "az", "za" };
        if (!validSort.Contains(sortKey)) sortKey = isSimilarMode ? "match" : "new";

        if (isSimilarMode && sortKey == "match")
        {
            cvs = cvs
                .OrderByDescending(c => c.MatchCount ?? 0)
                .ThenByDescending(c => c.CreatedUtc)
                .ThenBy(c => c.FullName)
                .ToList();
        }
        else
        {
            IOrderedEnumerable<SearchCvVm.CvCardVm> ordered = sortKey switch
            {
                "old" => cvs.OrderBy(c => c.CreatedUtc).ThenBy(c => c.FullName),
                "az" => cvs.OrderBy(c => c.FullName).ThenByDescending(c => c.CreatedUtc),
                "za" => cvs.OrderByDescending(c => c.FullName).ThenByDescending(c => c.CreatedUtc),
                _ => cvs.OrderByDescending(c => c.CreatedUtc).ThenBy(c => c.FullName),
            };

            if (isSimilarMode)
            {
                ordered = ordered.ThenByDescending(c => c.MatchCount ?? 0);
            }

            cvs = ordered.ToList();
        }

        var vm = new SearchCvVm
        {
            NameQuery = name ?? string.Empty,
            CityQuery = city ?? string.Empty,
            Mode = mode ?? "normal",
            Sort = sortKey,
            SortLockedToMatch = isSimilarMode,
            ShowLoginTip = !isAuthenticated,
            SelectedSkillIds = selectedSkillIds,
            SelectedSkillNames = selectedSkillNames,
            SimilarHint = isSimilarMode ? "Visar profiler som matchar dina kompetenser." : string.Empty,
            Source = source,
            SourceUserId = sourceUser,
            SimilarSourceTotal = sourceSkillIds.Length,
            Competences = competences,
            AllSkills = competences.Select(c => new SearchCvVm.SkillItemVm { Id = c.Id, Name = c.Name }).ToList(),
            Cvs = cvs
        };

        return View("SearchCV", vm);
    }

    private static int[] ParseSkillIds(string? skillIds)
    {
        if (string.IsNullOrWhiteSpace(skillIds)) return Array.Empty<int>();
        return skillIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .Distinct()
            .Take(10)
            .ToArray();
    }

    private static int[] ParseProjectIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<int>();
        try
        {
            var ids = System.Text.Json.JsonSerializer.Deserialize<int[]>(json) ?? Array.Empty<int>();
            return ids.Distinct().ToArray();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }
}

