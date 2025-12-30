using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;
using WebApp.Models.Export;

namespace WebApp.Controllers;

/// <summary>
/// Hanterar visning, export och meddelandefunktion för publika/anonyma användarprofiler (CV).
/// Innehåller loggning av besök, privacy-kontroller och server-side validering av meddelanden.
/// </summary>
[Route("InspectCV")]
public sealed class InspectCvController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public InspectCvController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // /InspectCV/{userId}
    [HttpGet("{userId}")]
    public async Task<IActionResult> ByUserId(string userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();

        // Integritet: avaktiverade profiler visas inte
        if (user.IsDeactivated)
        {
            return NotFound();
        }

        // Integritet: privata profiler kräver inloggning
        if (user.IsProfilePrivate && !(User.Identity?.IsAuthenticated == true))
        {
            return Forbid();
        }

        var link = await _db.ApplicationUserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        var profile = link is null
            ? null
            : await _db.Profiler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == link.ProfileId);

        // Logga profilbesök (spara rad per besök). Räkna inte ägarens egna besök.
        if (link is not null)
        {
            var visitorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (!string.Equals(visitorUserId, userId, StringComparison.Ordinal))
            {
                _db.ProfilBesok.Add(new ProfileVisit
                {
                    ProfileId = link.ProfileId,
                    VisitorUserId = string.IsNullOrWhiteSpace(visitorUserId) ? null : visitorUserId,
                    VisitorIp = ip,
                    VisitedUtc = DateTimeOffset.UtcNow
                });

                await _db.SaveChangesAsync();
            }
        }

        // Visa antal besök
        var visits = link is null
            ? 0
            : await _db.ProfilBesok.AsNoTracking().CountAsync(v => v.ProfileId == link.ProfileId);

        // Valda projekt från profilen
        var selectedProjectIds = ParseSelectedProjectIds(profile?.SelectedProjectsJson);

        var projects = new List<InspectCvProjectCardVm>();
        if (selectedProjectIds.Length > 0)
        {
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

                projects.Add(new InspectCvProjectCardVm
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

        var educations = link is null
            ? new List<InspectCvEducationVm>()
            : await _db.Utbildningar.AsNoTracking()
                .Where(x => x.ProfileId == link.ProfileId)
                .OrderBy(x => x.SortOrder)
                .Take(2)
                .Select(x => new InspectCvEducationVm
                {
                    School = x.School,
                    Program = x.Program,
                    Years = x.Years,
                    Note = x.Note
                })
                .ToListAsync();

        var experiences = link is null
            ? new List<InspectCvExperienceVm>()
            : await _db.Erfarenheter.AsNoTracking()
                .Where(x => x.ProfileId == link.ProfileId)
                .OrderBy(x => x.SortOrder)
                .Take(2)
                .Select(x => new InspectCvExperienceVm
                {
                    Company = x.Company,
                    Role = x.Role,
                    Years = x.Years,
                    Description = x.Description
                })
                .ToListAsync();

        var skills = await (from l in _db.ApplicationUserProfiles.AsNoTracking()
                            join uc in _db.AnvandarKompetenser.AsNoTracking() on l.UserId equals uc.UserId
                            join c in _db.Kompetenskatalog.AsNoTracking() on uc.CompetenceId equals c.Id
                            where l.UserId == userId
                            orderby c.SortOrder
                            select c.Name)
            .ToListAsync();

        var skillNames = skills.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // Meddelandeförifyllning: inloggade får namn förifyllt och låst, anonyma måste ange namn
        var viewer = User.Identity?.IsAuthenticated == true
            ? await _userManager.GetUserAsync(User)
            : null;

        var viewerName = viewer is null
            ? string.Empty
            : string.Join(' ', new[] { viewer.FirstName, viewer.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var vm = new InspectCvViewModel
        {
            UserId = user.Id,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            IsPrivate = user.IsProfilePrivate,

            City = user.City,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty,

            VisitCount = visits,

            Headline = profile?.Headline,
            AboutMe = profile?.AboutMe,
            ProfileImagePath = profile?.ProfileImagePath ?? user.ProfileImagePath,
            Skills = skillNames,

            Educations = educations,
            Experiences = experiences,

            Projects = projects,

            MessagePrefillName = viewerName,
            MessageNameReadonly = viewer is not null
        };

        return View("InspectCV", vm);
    }

    // GET: /InspectCV/{userId}/Export
    [HttpGet("{userId}/Export")]
    public async Task<IActionResult> Export(string userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return NotFound();
        if (user.IsDeactivated) return NotFound();

        // Samma privacy-guard som vy
        if (user.IsProfilePrivate && !(User.Identity?.IsAuthenticated == true))
        {
            return Forbid();
        }

        var link = await _db.ApplicationUserProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        var profile = link is null
            ? null
            : await _db.Profiler.AsNoTracking().FirstOrDefaultAsync(p => p.Id == link.ProfileId);

        // Full CV-data för export
        var educations = link is null
            ? new List<ExportEducationDto>()
            : await _db.Utbildningar.AsNoTracking()
                .Where(x => x.ProfileId == link.ProfileId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ExportEducationDto
                {
                    School = x.School,
                    Program = x.Program,
                    Years = x.Years,
                    Note = x.Note
                })
                .ToListAsync();

        var experiences = link is null
            ? new List<ExportExperienceDto>()
            : await _db.Erfarenheter.AsNoTracking()
                .Where(x => x.ProfileId == link.ProfileId)
                .OrderBy(x => x.SortOrder)
                .Select(x => new ExportExperienceDto
                {
                    Company = x.Company,
                    Role = x.Role,
                    Years = x.Years,
                    Description = x.Description
                })
                .ToListAsync();

        var skills = await (from l in _db.ApplicationUserProfiles.AsNoTracking()
                            join uc in _db.AnvandarKompetenser.AsNoTracking() on l.UserId equals uc.UserId
                            join c in _db.Kompetenskatalog.AsNoTracking() on uc.CompetenceId equals c.Id
                            where l.UserId == userId
                            orderby c.SortOrder
                            select c.Name)
            .ToListAsync();

        var skillNames = skills.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var selectedProjectIds = ParseSelectedProjectIds(profile?.SelectedProjectsJson);
        var exportProjects = new List<ExportProjectDto>();


        if (selectedProjectIds.Length > 0)
        {
            var projects = await _db.Projekt.AsNoTracking()
                .Where(p => selectedProjectIds.Contains(p.Id))
                .ToListAsync();

            var map = projects.ToDictionary(p => p.Id);
            foreach (var id in selectedProjectIds)
            {
                if (!map.TryGetValue(id, out var p)) continue;

                exportProjects.Add(new ExportProjectDto
                {
                    ProjectId = p.Id,
                    Title = p.Titel,
                    ShortDescription = p.KortBeskrivning,
                    Description = p.Beskrivning,
                    CreatedUtc = p.CreatedUtc,
                    ImagePath = p.ImagePath,
                    TechKeys = ParseCsv(p.TechStackKeysCsv).ToList()
                });
            }
        }

        var dto = new ExportProfileDto
        {
            User = new ExportUserDto
            {
                UserId = user.Id,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                City = user.City,
                Email = user.Email,
                Phone = user.PhoneNumberDisplay ?? user.PhoneNumber
            },
            Cv = new ExportCvDto
            {
                Headline = profile?.Headline,
                AboutMe = profile?.AboutMe,
                ProfileImagePath = profile?.ProfileImagePath ?? user.ProfileImagePath,
                Skills = skillNames.ToList(),
                Educations = educations,
                Experiences = experiences
            },

            Projects = exportProjects
        };

        var serializer = new XmlSerializer(typeof(ExportProfileDto));

        await using var ms = new MemoryStream();
        await using (var writer = new StreamWriter(ms, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true))
        {
            serializer.Serialize(writer, dto);
        }

        ms.Position = 0;

        // Bygg ett säkert filnamn utan specialtecken
        var safeName = (user.FirstName + "_" + user.LastName).Trim('_');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "profile";
        safeName = string.Concat(safeName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));

        var fileName = $"notlinkedin_profile_{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmm}.xml";
        return File(ms.ToArray(), "application/xml", fileName);
    }

    // POST: /InspectCV/{userId}/SendMessage
    [HttpPost("{userId}/SendMessage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(string userId, [FromForm] SendMessageInput input)
    {
        var recipient = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        if (recipient is null) return NotFound();

        if (recipient.IsDeactivated)
        {
            return NotFound();
        }

        if (recipient.IsProfilePrivate && !(User.Identity?.IsAuthenticated == true))
        {
            return Forbid();
        }

        // Server-side validering; vid fel -> återgå till vy
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(ByUserId), new { userId });
        }

        var viewerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? senderUserId = null;
        string? senderName = null;

        if (!string.IsNullOrWhiteSpace(viewerUserId))
        {
            // Inloggad: använd kontots namn och id (låst)
            var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == viewerUserId);
            if (sender is not null)
            {
                senderUserId = sender.Id;
                senderName = string.Join(' ', new[] { sender.FirstName, sender.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
        }
        else
        {
            // Anonym: namn måste anges i formuläret
            senderName = input.SenderName?.Trim();
        }

        // Grundläggande säkerhetskontroller för namn och meddelandelängd
        if (string.IsNullOrWhiteSpace(senderName) || !IsValidPersonName(senderName))
        {
            return RedirectToAction(nameof(ByUserId), new { userId });
        }

        var body = input.Body?.Trim() ?? string.Empty;
        if (body.Length is < 1 or > 500)
        {
            return RedirectToAction(nameof(ByUserId), new { userId });
        }

        _db.UserMessages.Add(new UserMessage
        {
            RecipientUserId = recipient.Id,
            SenderUserId = senderUserId,
            SenderName = senderName,
            Subject = string.Empty,
            Body = body,
            SentUtc = DateTimeOffset.UtcNow,
            IsRead = false
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(ByUserId), new { userId });
    }

    private static bool IsValidPersonName(string name)
    {
        // Tillåt bokstäver (inkl. svenska), mellanslag och bindestreck; inga siffror eller symboler.
        if (name.Length is < 1 or > 100) return false;

        foreach (var ch in name)
        {
            if (char.IsLetter(ch) || ch == '-' || ch == ' ')
            {
                continue;
            }
            return false;
        }

        return true;
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

    public sealed class SendMessageInput
    {
        [StringLength(100)]
        public string? SenderName { get; init; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Body { get; init; } = string.Empty;
    }

    public sealed class InspectCvProjectCardVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }
        public string? ImagePath { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public string[] TechKeys { get; init; } = Array.Empty<string>();
    }

    public sealed class InspectCvEducationVm
    {
        public string School { get; init; } = string.Empty;
        public string Program { get; init; } = string.Empty;
        public string Years { get; init; } = string.Empty;
        public string? Note { get; init; }
    }

    public sealed class InspectCvExperienceVm
    {
        public string Company { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public string Years { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    public sealed class InspectCvViewModel
    {
        public string UserId { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool IsPrivate { get; init; }

        public int VisitCount { get; init; }

        public string City { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Phone { get; init; } = string.Empty;

        public string? Headline { get; init; }
        public string? AboutMe { get; init; }
        public string? ProfileImagePath { get; init; }
        public string[] Skills { get; init; } = Array.Empty<string>();

        public List<InspectCvEducationVm> Educations { get; init; } = new();
        public List<InspectCvExperienceVm> Experiences { get; init; } = new();

        public List<InspectCvProjectCardVm> Projects { get; init; } = new();

        // Meddelandeform
        public string MessagePrefillName { get; init; } = string.Empty;
        public bool MessageNameReadonly { get; init; }

        public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
