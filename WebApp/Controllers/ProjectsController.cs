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

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? q, [FromQuery] string? sort)
    {
        // Join to creator so we can filter on name/email and (optionally) show it.
        var query = from p in _db.Projekt.AsNoTracking()
                    join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                    from u in users.DefaultIfEmpty()
                    select new { p, u };

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();

            // EF.Functions.Like makes it case-insensitive on SQL Server collations typically,
            // and avoids client-side evaluation.
            var like = $"{s}%";
            var likeAnywhere = $"%{s}%";

            query = query.Where(x =>
                EF.Functions.Like(x.p.Titel, likeAnywhere) ||
                (x.p.KortBeskrivning != null && EF.Functions.Like(x.p.KortBeskrivning, likeAnywhere)) ||
                (x.u != null && x.u.FirstName != null && EF.Functions.Like(x.u.FirstName, like)) ||
                (x.u != null && x.u.Email != null && EF.Functions.Like(x.u.Email, like)));
        }

        var sortKey = (sort ?? "new").ToLowerInvariant();
        query = sortKey switch
        {
            "old" => query.OrderBy(x => x.p.CreatedUtc),
            "az" => query.OrderBy(x => x.p.Titel),
            "za" => query.OrderByDescending(x => x.p.Titel),
            _ => query.OrderByDescending(x => x.p.CreatedUtc)
        };

        var items = await query
            .Select(x => new ProjectsIndexVm.ProjectCardVm
            {
                Id = x.p.Id,
                Title = x.p.Titel,
                ShortDescription = x.p.KortBeskrivning,
                CreatedUtc = x.p.CreatedUtc,
                TechKeysCsv = x.p.TechStackKeysCsv,
                CreatedByName = x.u == null ? null : ((x.u.FirstName + " " + x.u.LastName).Trim()),
                CreatedByEmail = x.u == null ? null : x.u.Email
            })
            .ToListAsync();

        var vm = new ProjectsIndexVm
        {
            Query = q ?? string.Empty,
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
            CreatedByUserId = user.Id,
            CreatedUtc = now,
            UpdatedUtc = now
        };

        _db.Projekt.Add(project);
        await _db.SaveChangesAsync();

        // Owner auto-joins their own project
        _db.ProjektAnvandare.Add(new ProjectUser { ProjectId = project.Id, UserId = user.Id, ConnectedUtc = now });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _db.Projekt.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();

        var viewer = await _userManager.GetUserAsync(User);
        var isLoggedIn = viewer is not null;

        var isOwner = isLoggedIn && project.CreatedByUserId == viewer!.Id;

        // Participants: hide private users if viewer is anonymous.
        var participantsQuery = from pu in _db.ProjektAnvandare.AsNoTracking()
                                join u in _db.Users.AsNoTracking() on pu.UserId equals u.Id
                                where pu.ProjectId == id
                                select new { u.Id, u.FirstName, u.LastName, u.City, u.IsProfilePrivate };

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
                City = x.City
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

        // Safety max length
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
