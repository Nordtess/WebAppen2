using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;
using WebApp.ViewModels;

namespace WebApp.Controllers;

/// <summary>
/// Hanterar CRUD- och listningsfunktionalitet för projekt.
/// </summary>
public sealed class ProjectsController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProjectsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var project = await _db.Projekt.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        if (project.CreatedByUserId != user.Id) return Forbid();

        // Ta bort medlemskopplingar först för att undvika FK-konstigheter vid radering
        var links = await _db.ProjektAnvandare.Where(x => x.ProjectId == id).ToListAsync();
        if (links.Count > 0)
        {
            _db.ProjektAnvandare.RemoveRange(links);
        }

        _db.Projekt.Remove(project);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? sort, [FromQuery] string? scope, [FromQuery] bool? mine)
    {
        var viewer = await _userManager.GetUserAsync(User);
        var viewerId = viewer?.Id;

        var scopeKey = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();
        if (scopeKey is not ("all" or "title" or "created" or "member"))
        {
            scopeKey = "all";
        }

        var onlyMine = (mine ?? false) && viewerId != null;

        // Basfråga: join mot skapare för att kunna visa och filtrera på namn/email
        var baseQuery = from p in _db.Projekt.AsNoTracking()
                        join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                        from u in users.DefaultIfEmpty()
                        select new { p, u };

        if (onlyMine)
        {
            baseQuery = baseQuery.Where(x =>
                x.p.CreatedByUserId == viewerId ||
                _db.ProjektAnvandare.AsNoTracking().Any(pu => pu.ProjectId == x.p.Id && pu.UserId == viewerId));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();

            // EF.Functions.Like används för att förlita sig på DB-kollation för case-insensitiv match och undvika klientutvärdering.
            var like = $"{s}%";
            var likeAnywhere = $"%{s}%";

            if (scopeKey == "title")
            {
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.p.Titel, likeAnywhere) ||
                    (x.p.KortBeskrivning != null && EF.Functions.Like(x.p.KortBeskrivning, likeAnywhere)));
            }
            else if (scopeKey == "created")
            {
                baseQuery = baseQuery.Where(x =>
                    (x.u != null && x.u.FirstName != null && EF.Functions.Like(x.u.FirstName, like)) ||
                    (x.u != null && x.u.LastName != null && EF.Functions.Like(x.u.LastName, like)) ||
                    (x.u != null && x.u.Email != null && EF.Functions.Like(x.u.Email, like)));
            }
            else if (scopeKey == "member")
            {
                // Sök i medlemmar via subquery för att hålla huvudprojektraden kompakt
                baseQuery = baseQuery.Where(x =>
                    _db.ProjektAnvandare.AsNoTracking()
                        .Where(pu => pu.ProjectId == x.p.Id)
                        .Join(_db.Users.AsNoTracking(), pu => pu.UserId, u2 => u2.Id, (pu, u2) => u2)
                        .Any(u2 =>
                            (u2.FirstName != null && EF.Functions.Like(u2.FirstName, like)) ||
                            (u2.LastName != null && EF.Functions.Like(u2.LastName, like)) ||
                            (u2.Email != null && EF.Functions.Like(u2.Email, like))));
            }
            else
            {
                // all: sök i titel, kort beskrivning, skapare och medlemmar
                baseQuery = baseQuery.Where(x =>
                    EF.Functions.Like(x.p.Titel, likeAnywhere) ||
                    (x.p.KortBeskrivning != null && EF.Functions.Like(x.p.KortBeskrivning, likeAnywhere)) ||
                    (x.u != null && x.u.FirstName != null && EF.Functions.Like(x.u.FirstName, like)) ||
                    (x.u != null && x.u.LastName != null && EF.Functions.Like(x.u.LastName, like)) ||
                    (x.u != null && x.u.Email != null && EF.Functions.Like(x.u.Email, like)) ||
                    _db.ProjektAnvandare.AsNoTracking()
                        .Where(pu => pu.ProjectId == x.p.Id)
                        .Join(_db.Users.AsNoTracking(), pu => pu.UserId, u2 => u2.Id, (pu, u2) => u2)
                        .Any(u2 =>
                            (u2.FirstName != null && EF.Functions.Like(u2.FirstName, like)) ||
                            (u2.LastName != null && EF.Functions.Like(u2.LastName, like)) ||
                            (u2.Email != null && EF.Functions.Like(u2.Email, like))));
            }
        }

        var sortKey = (sort ?? "new").ToLowerInvariant();
        baseQuery = sortKey switch
        {
            "old" => baseQuery.OrderBy(x => x.p.CreatedUtc),
            "az" => baseQuery.OrderBy(x => x.p.Titel),
            "za" => baseQuery.OrderByDescending(x => x.p.Titel),
            _ => baseQuery.OrderByDescending(x => x.p.CreatedUtc)
        };

        var items = await baseQuery
             .Select(x => new ProjectsIndexVm.ProjectCardVm
             {
                 Id = x.p.Id,
                 Title = x.p.Titel,
                 ShortDescription = x.p.KortBeskrivning,
                 CreatedUtc = x.p.CreatedUtc,
                 TechKeysCsv = x.p.TechStackKeysCsv,
                 ImagePath = x.p.ImagePath,
                 CreatedByName = x.u == null
                     ? null
                     : (((x.u.FirstName ?? "") + " " + (x.u.LastName ?? "")).Trim() == ""
                         ? null
                         : (((x.u.FirstName ?? "") + " " + (x.u.LastName ?? "")).Trim())),
                 CreatedByEmail = x.u == null ? "" : (x.u.Email ?? "")
             })
             .ToListAsync();

        var vm = new ProjectsIndexVm
        {
            Query = q ?? string.Empty,
            Scope = scopeKey,
            OnlyMine = onlyMine,
            Sort = sortKey,
            ShowLoginTip = !(User.Identity?.IsAuthenticated ?? false),
            Projects = items
        };

        return View("Index", vm);
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        return View("Create", new ProjectEditViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            return View("Create", model);
        }

        var now = DateTimeOffset.UtcNow;

        var project = new Project
        {
            Titel = model.Title.Trim(),
            KortBeskrivning = string.IsNullOrWhiteSpace(model.ShortDescription) ? null : model.ShortDescription.Trim(),
            Beskrivning = model.Description.Trim(),
            TechStackKeysCsv = NormalizeTechJsonToCsv(model.TechStackJson),
            ImagePath = string.IsNullOrWhiteSpace(model.ProjectImage) ? null : model.ProjectImage.Trim(),
            CreatedByUserId = user.Id,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _db.Projekt.Add(project);
        await _db.SaveChangesAsync();

        // Koppla skaparen som medlem så att relationen finns (ägarskap hanteras separat i UI)
        _db.ProjektAnvandare.Add(new ProjectUser { ProjectId = project.Id, UserId = user.Id, ConnectedUtc = now });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Hämta projekt + skapare i en fråga för detaljvisning
        var projectRow = await (from p in _db.Projekt.AsNoTracking()
                                join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                                from u in users.DefaultIfEmpty()
                                where p.Id == id
                                select new { p, u })
            .FirstOrDefaultAsync();

        if (projectRow is null) return NotFound();

        var project = projectRow.p;
        var creator = projectRow.u;

        var viewer = await _userManager.GetUserAsync(User);
        var isLoggedIn = viewer is not null;

        var isOwner = isLoggedIn && project.CreatedByUserId == viewer!.Id;

        // Deltagare: döljer privata profiler för anonyma tittare
        var participantsQuery = from pu in _db.ProjektAnvandare.AsNoTracking()
                                join u in _db.Users.AsNoTracking() on pu.UserId equals u.Id
                                join link in _db.ApplicationUserProfiles.AsNoTracking() on u.Id equals link.UserId into links
                                from link in links.DefaultIfEmpty()
                                join prof in _db.Profiler.AsNoTracking() on link.ProfileId equals prof.Id into profs
                                from prof in profs.DefaultIfEmpty()
                                where pu.ProjectId == id
                                where pu.UserId != project.CreatedByUserId
                                where !u.IsDeactivated
                                select new { u.Id, u.FirstName, u.LastName, u.City, u.IsProfilePrivate, Headline = prof != null ? prof.Headline : null };

        if (!isLoggedIn)
        {
            participantsQuery = participantsQuery.Where(x => !x.IsProfilePrivate);
        }

        var participants = await participantsQuery
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .Select(x => new ProjectDetailsVm.ParticipantVm
            {
                UserId = x.Id,
                FullName = (x.FirstName + " " + x.LastName).Trim(),
                City = x.City,
                Headline = x.Headline
            })
            .ToListAsync();

        var isMember = false;
        if (isLoggedIn)
        {
            isMember = await _db.ProjektAnvandare.AsNoTracking().AnyAsync(x => x.ProjectId == id && x.UserId == viewer!.Id);
        }

        var vm = new ProjectDetailsVm
        {
            Id = project.Id,
            Title = project.Titel,
            ShortDescription = project.KortBeskrivning,
            Description = project.Beskrivning,
            ImagePath = project.ImagePath,
            CreatedByName = creator == null
                ? null
                : (string.IsNullOrWhiteSpace(((creator.FirstName ?? "") + " " + (creator.LastName ?? "")).Trim())
                    ? null
                    : ((creator.FirstName ?? "") + " " + (creator.LastName ?? "")).Trim()),
            CreatedByEmail = creator?.Email ?? "Okänd",
            CreatedUtc = project.CreatedUtc,
            CreatedByUserId = project.CreatedByUserId,
            TechKeys = ParseCsv(project.TechStackKeysCsv),

            IsOwner = isOwner,
            IsMember = isMember,
            Participants = participants
        };

        return View("Details", vm);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var project = await _db.Projekt.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        if (project.CreatedByUserId != user.Id) return Forbid();

        var vm = new ProjectEditViewModel
        {
            Id = project.Id,
            Title = project.Titel,
            ShortDescription = project.KortBeskrivning,
            Description = project.Beskrivning,
            TechStackJson = JsonSerializer.Serialize(ParseCsv(project.TechStackKeysCsv), JsonOptions),
            ProjectImage = project.ImagePath,
            CreatedText = $"Skapad: {project.CreatedUtc:yyyy-MM-dd}",
            IsOwner = true
        };

        return View("Edit", vm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectEditViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var project = await _db.Projekt.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (project is null) return NotFound();

        if (project.CreatedByUserId != user.Id) return Forbid();

        if (!ModelState.IsValid)
        {
            model.CreatedText = $"Skapad: {project.CreatedUtc:yyyy-MM-dd}";
            model.IsOwner = true;
            return View("Edit", model);
        }

        project.Titel = model.Title.Trim();
        project.KortBeskrivning = string.IsNullOrWhiteSpace(model.ShortDescription) ? null : model.ShortDescription.Trim();
        project.Beskrivning = model.Description.Trim();
        project.TechStackKeysCsv = NormalizeTechJsonToCsv(model.TechStackJson);
        project.ImagePath = string.IsNullOrWhiteSpace(model.ProjectImage) ? null : model.ProjectImage.Trim();
        project.UpdatedUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var exists = await _db.Projekt.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound();

        var already = await _db.ProjektAnvandare.AnyAsync(x => x.ProjectId == id && x.UserId == user.Id);
        if (!already)
        {
            _db.ProjektAnvandare.Add(new ProjectUser { ProjectId = id, UserId = user.Id, ConnectedUtc = DateTimeOffset.UtcNow });
            await _db.SaveChangesAsync();

            TempData["ToastTitle"] = "Du gick med i ett projekt";
            TempData["ToastMessage"] = "Du har gått med i projektet! Glöm inte att lägga till det på ditt CV för att visa andra!";
        }
        else
        {
            TempData["ToastTitle"] = "Du är redan medlem";
            TempData["ToastMessage"] = "Du är redan kopplad till det här projektet.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var project = await _db.Projekt.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        if (project.CreatedByUserId == user.Id) return Forbid();

        var link = await _db.ProjektAnvandare.FirstOrDefaultAsync(x => x.ProjectId == id && x.UserId == user.Id);
        if (link is not null)
        {
            _db.ProjektAnvandare.Remove(link);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static string? NormalizeTechJsonToCsv(string? techJson)
    {
        if (string.IsNullOrWhiteSpace(techJson)) return null;

        string[] items;
        try
        {
            items = JsonSerializer.Deserialize<string[]>(techJson, JsonOptions) ?? Array.Empty<string>();
        }
        catch
        {
            return null;
        }

        var normalized = items
            .Select(s => (s ?? string.Empty).Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0) return null;

        // Säkerhetsgräns för total längd så fältet inte överskrider databasgräns
        var sb = new StringBuilder();
        foreach (var n in normalized)
        {
            if (sb.Length > 0) sb.Append(',');
            if (sb.Length + n.Length > 500) break;
            sb.Append(n);
        }

        return sb.ToString();
    }

    private static string[] ParseCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return Array.Empty<string>();

        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
