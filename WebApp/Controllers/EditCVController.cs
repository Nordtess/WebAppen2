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

[Authorize]
public class EditCVController : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public EditCVController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var (profile, _) = await GetOrCreateProfileForUserAsync(user.Id);

        var educations = await _db.Utbildningar
            .AsNoTracking()
            .Where(x => x.ProfileId == profile.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new EducationItemVm
            {
                School = x.School,
                Program = x.Program,
                Years = x.Years,
                Note = x.Note
            })
            .ToListAsync();

        var selectedIds = ParseSelectedProjectIds(profile.SelectedProjectsJson);
        var allMyProjects = await GetAllMyProjectsAsync(user.Id);

        var vm = new EditCvViewModel
        {
            FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty,
            Location = user.City ?? string.Empty,

            Headline = profile.Headline,
            AboutMe = profile.AboutMe ?? string.Empty,
            ProfileImagePath = profile.ProfileImagePath,

            // education is now stored in table
            EducationJson = JsonSerializer.Serialize(educations, JsonOptions),

            // projects
            SelectedProjectsJson = string.IsNullOrWhiteSpace(profile.SelectedProjectsJson) ? "[]" : profile.SelectedProjectsJson!,
            SelectedProjectIds = selectedIds.Take(4).ToArray(),
            AllMyProjects = allMyProjects,

            SkillsJson = SkillsCsvToJson(profile.SkillsCsv)
        };

        // Det som visas på GET är det som finns i DB => "saved" som initialt state.
        TempData["Saved"] ??= "1";

        return View("EditCV", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EditCvViewModel model, [FromForm(Name = "AvatarFile")] IFormFile? avatarFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        // Read-only fält ska alltid komma från DB (skydd mot tampering).
        model.FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        model.Email = user.Email ?? string.Empty;
        model.Phone = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty;
        model.Location = user.City ?? string.Empty;

        if (!ModelState.IsValid)
        {
            // Vid valideringsfel: behåll "dirty".
            TempData.Remove("Saved");
            return View("EditCV", model);
        }

        // If user is creating their CV for the first time from MyCV,
        // MyCvController sets this flag so we can redirect them back after successful save.
        var redirectToMyCvAfterSave = TempData["FirstCvEdit"]?.ToString() == "1";

        var (profile, _) = await GetOrCreateProfileForUserAsync(user.Id);

        await using var tx = await _db.Database.BeginTransactionAsync();

        profile.Headline = string.IsNullOrWhiteSpace(model.Headline) ? null : model.Headline.Trim();
        profile.AboutMe = model.AboutMe.Trim();
        profile.SelectedProjectsJson = string.IsNullOrWhiteSpace(model.SelectedProjectsJson) ? "[]" : model.SelectedProjectsJson;
        profile.SkillsCsv = NormalizeSkillsJsonToCsv(model.SkillsJson);

        // Replace educations (delete + insert) => simple & reliable.
        await ReplaceEducationsAsync(profile.Id, model.EducationJson);

        if (avatarFile is not null && avatarFile.Length > 0)
        {
            var path = await SaveAvatarAsync(user.Id, avatarFile);
            profile.ProfileImagePath = path;

            // Behåll även på användaren (om andra vyer redan läser därifrån).
            user.ProfileImagePath = path;
            await _userManager.UpdateAsync(user);
        }

        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        TempData["Saved"] = "1";

        if (redirectToMyCvAfterSave)
        {
            // Redirect them to MyCV after the first successful save so they see the result.
            return RedirectToAction("Index", "MyCV");
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task ReplaceEducationsAsync(int profileId, string? educationJson)
    {
        // Delete existing
        var existing = await _db.Utbildningar.Where(x => x.ProfileId == profileId).ToListAsync();
        if (existing.Count > 0)
        {
            _db.Utbildningar.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        // Insert new
        var items = DeserializeEducationItems(educationJson);
        if (items.Count == 0)
        {
            return;
        }

        // hard limits for safety
        const int maxItems = 12;
        var now = DateTimeOffset.UtcNow;

        var toInsert = new List<Education>();
        for (var i = 0; i < items.Count && i < maxItems; i++)
        {
            var it = items[i];

            var school = (it.School ?? string.Empty).Trim();
            var program = (it.Program ?? string.Empty).Trim();
            var years = (it.Years ?? string.Empty).Trim();
            var note = string.IsNullOrWhiteSpace(it.Note) ? null : it.Note.Trim();

            if (string.IsNullOrWhiteSpace(school) || string.IsNullOrWhiteSpace(program) || string.IsNullOrWhiteSpace(years))
            {
                continue;
            }

            // Truncate to DB limits (airtight)
            school = school.Length > 120 ? school[..120] : school;
            program = program.Length > 120 ? program[..120] : program;
            years = years.Length > 40 ? years[..40] : years;
            if (note is not null && note.Length > 200) note = note[..200];

            toInsert.Add(new Education
            {
                ProfileId = profileId,
                School = school,
                Program = program,
                Years = years,
                Note = note,
                SortOrder = i,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        if (toInsert.Count == 0) return;

        _db.Utbildningar.AddRange(toInsert);
        await _db.SaveChangesAsync();
    }

    private static List<EducationItemVm> DeserializeEducationItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();

        try
        {
            return JsonSerializer.Deserialize<List<EducationItemVm>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private async Task<(Profile profile, ApplicationUserProfile link)> GetOrCreateProfileForUserAsync(string userId)
    {
        var link = await _db.ApplicationUserProfiles
            .Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (link is not null)
        {
            return (link.Profile, link);
        }

        var profile = new Profile
        {
            OwnerUserId = userId,
            IsPublic = true,
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow,
            SelectedProjectsJson = "[]",
            SkillsCsv = null
        };

        _db.Profiler.Add(profile);
        await _db.SaveChangesAsync();

        link = new ApplicationUserProfile
        {
            UserId = userId,
            ProfileId = profile.Id,
            Profile = profile
        };

        _db.ApplicationUserProfiles.Add(link);
        await _db.SaveChangesAsync();

        return (profile, link);
    }

    private static string SkillsCsvToJson(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return "[]";

        var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static string? NormalizeSkillsJsonToCsv(string? skillsJson)
    {
        if (string.IsNullOrWhiteSpace(skillsJson)) return null;

        string[] items;
        try
        {
            items = JsonSerializer.Deserialize<string[]>(skillsJson, JsonOptions) ?? Array.Empty<string>();
        }
        catch
        {
            return null;
        }

        var normalized = items
            .Select(s => (s ?? string.Empty).Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0) return null;

        // Begränsa så vi inte kan spara obegränsat stora strängar.
        var sb = new StringBuilder();
        foreach (var n in normalized)
        {
            if (sb.Length > 0) sb.Append(',');
            if (sb.Length + n.Length > 1000) break;
            sb.Append(n);
        }

        return sb.ToString();
    }

    private async Task<string> SaveAvatarAsync(string userId, IFormFile file)
    {
        // Enkel och robust: spara som original extension, men skydda filnamn.
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".png";

        ext = ext.ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif" };
        if (!allowed.Contains(ext))
        {
            ext = ".png";
        }

        var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads", "avatars", userId);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"avatar_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}{ext}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        // Web path
        return $"/uploads/avatars/{userId}/{fileName}";
    }

    private static int[] ParseSelectedProjectIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<int>();

        try
        {
            return JsonSerializer.Deserialize<int[]>(json, JsonOptions) ?? Array.Empty<int>();
        }
        catch
        {
            return Array.Empty<int>();
        }
    }

    private async Task<List<EditCvProjectPickVm>> GetAllMyProjectsAsync(string userId)
    {
        var q = from p in _db.Projekt.AsNoTracking()
                where p.CreatedByUserId == userId
                   || _db.ProjektAnvandare.AsNoTracking().Any(pu => pu.ProjectId == p.Id && pu.UserId == userId)
                select new EditCvProjectPickVm
                {
                    Id = p.Id,
                    Title = p.Titel,
                    CreatedUtc = p.CreatedUtc
                };

        return await q
            .OrderByDescending(x => x.CreatedUtc)
            .ThenBy(x => x.Title)
            .ToListAsync();
    }
}
