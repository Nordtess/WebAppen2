using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Helpers;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;
using WebApp.ViewModels;

namespace WebApp.Controllers;

/// <summary>
/// Hanterar redigering och lagring av användarens CV/profil (inkl. utbildning, erfarenhet, projekt och avatar).
/// </summary>
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
    public async Task<IActionResult> Index([FromQuery] string? toastTitle = null, [FromQuery] string? toastMessage = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!string.IsNullOrWhiteSpace(toastMessage))
        {
            ViewData["ToastTitle"] = toastTitle;
            ViewData["ToastMessage"] = toastMessage;
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

        var experiences = await _db.Erfarenheter
            .AsNoTracking()
            .Where(x => x.ProfileId == profile.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new WorkExperienceItemVm
            {
                Company = x.Company,
                Role = x.Role,
                Years = x.Years,
                Description = x.Description
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

            // education is stored in table
            EducationJson = JsonSerializer.Serialize(educations, JsonOptions),

            // experience is stored in table
            ExperienceJson = JsonSerializer.Serialize(experiences, JsonOptions),

            // projects
            SelectedProjectsJson = string.IsNullOrWhiteSpace(profile.SelectedProjectsJson) ? "[]" : profile.SelectedProjectsJson!,
            SelectedProjectIds = selectedIds.Take(4).ToArray(),
            AllMyProjects = allMyProjects,

            SkillsJson = SkillsCsvToJson(profile.SkillsCsv)
        };

        // Markera initialt att data i vyn är "saved" (det som visas på GET är innehållet i DB).
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

        // Läs-only fält får alltid komma från DB för att förhindra tampering.
        model.FullName = string.Join(' ', new[] { user.FirstName, user.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        model.Email = user.Email ?? string.Empty;
        model.Phone = user.PhoneNumberDisplay ?? user.PhoneNumber ?? string.Empty;
        model.Location = user.City ?? string.Empty;

        // Validera och normalisera år-format innan ModelState-check.
        if (!TryValidateEducationYears(model.EducationJson, out var educationYearsError))
        {
            ModelState.AddModelError(string.Empty, educationYearsError);
        }

        if (!TryValidateExperienceYears(model.ExperienceJson, out var expYearsError))
        {
            ModelState.AddModelError(string.Empty, expYearsError);
        }

        if (!ModelState.IsValid)
        {
            // Vid valideringsfel: behåll "dirty"-status.
            TempData.Remove("Saved");
            return View("EditCV", model);
        }

        // Om detta är första gången användaren skapar ett CV från MyCV-flödet.
        var isFirstCvEdit = TempData.Peek("FirstCvEdit")?.ToString() == "1";

        var (profile, _) = await GetOrCreateProfileForUserAsync(user.Id);

        await using var tx = await _db.Database.BeginTransactionAsync();

        profile.Headline = string.IsNullOrWhiteSpace(model.Headline) ? null : model.Headline.Trim();
        profile.AboutMe = model.AboutMe.Trim();
        profile.SelectedProjectsJson = string.IsNullOrWhiteSpace(model.SelectedProjectsJson) ? "[]" : model.SelectedProjectsJson;
        profile.SkillsCsv = NormalizeSkillsJsonToCsv(model.SkillsJson);

        await ReplaceEducationsAsync(profile.Id, model.EducationJson);
        await ReplaceExperiencesAsync(profile.Id, model.ExperienceJson);

        if (avatarFile is not null && avatarFile.Length > 0)
        {
            var path = await SaveAvatarAsync(user.Id, avatarFile);
            profile.ProfileImagePath = path;

            // Spara även i ApplicationUser så andra vyer som läser där får uppdaterad path.
            user.ProfileImagePath = path;
            await _userManager.UpdateAsync(user);
        }

        profile.UpdatedUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        await tx.CommitAsync();

        TempData["Saved"] = "1";

        // Rensa första-CV-flaggan om den fanns
        if (isFirstCvEdit)
        {
            TempData.Remove("FirstCvEdit");
        }

        // Persist onboarding-flagga: användaren har nu skapat ett CV.
        if (!user.HasCreatedCv)
        {
            user.HasCreatedCv = true;
            await _userManager.UpdateAsync(user);
        }

        // Efter lyckad spara, återvänd alltid till MyCV.
        return RedirectToAction("Index", "MyCV");
    }

    private async Task ReplaceEducationsAsync(int profileId, string? educationJson)
    {
        // Ta bort tidigare poster
        var existing = await _db.Utbildningar.Where(x => x.ProfileId == profileId).ToListAsync();
        if (existing.Count > 0)
        {
            _db.Utbildningar.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        // Lägg till nya poster från JSON
        var items = DeserializeEducationItems(educationJson);
        if (items.Count == 0)
        {
            return;
        }

        // Hårda gränser för säkerhet
        const int maxItems = 12;
        var now = DateTimeOffset.UtcNow;

        var toInsert = new List<Education>();
        for (var i = 0; i < items.Count && i < maxItems; i++)
        {
            var it = items[i];

            var school = NameNormalizer.ToDisplayName((it.School ?? string.Empty).Trim());
            var program = NameNormalizer.ToDisplayName((it.Program ?? string.Empty).Trim());
            var years = NormalizeYearRange((it.Years ?? string.Empty).Trim());
            var note = string.IsNullOrWhiteSpace(it.Note) ? null : NameNormalizer.ToDisplayName(it.Note.Trim());

            if (string.IsNullOrWhiteSpace(school) || string.IsNullOrWhiteSpace(program) || string.IsNullOrWhiteSpace(years))
            {
                continue;
            }

            // Trunkera enligt DB-gränser
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

    private async Task ReplaceExperiencesAsync(int profileId, string? experienceJson)
    {
        // Ta bort tidigare poster
        var existing = await _db.Erfarenheter.Where(x => x.ProfileId == profileId).ToListAsync();
        if (existing.Count > 0)
        {
            _db.Erfarenheter.RemoveRange(existing);
            await _db.SaveChangesAsync();
        }

        // Lägg till nya poster från JSON
        var items = DeserializeExperienceItems(experienceJson);
        if (items.Count == 0) return;

        // Hårda gränser för säkerhet
        const int maxItems = 12;
        var now = DateTimeOffset.UtcNow;

        var toInsert = new List<WorkExperience>();
        for (var i = 0; i < items.Count && i < maxItems; i++)
        {
            var it = items[i];

            var company = NameNormalizer.ToDisplayName((it.Company ?? string.Empty).Trim());
            var role = NameNormalizer.ToDisplayName((it.Role ?? string.Empty).Trim());
            var years = NormalizeYearRange((it.Years ?? string.Empty).Trim());
            var desc = string.IsNullOrWhiteSpace(it.Description) ? null : it.Description.Trim();

            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(years))
            {
                continue;
            }

            if (desc is not null)
            {
                // Komprimera dubbla mellanslag och trunkera
                while (desc.Contains("  ", StringComparison.Ordinal))
                {
                    desc = desc.Replace("  ", " ", StringComparison.Ordinal);
                }

                if (desc.Length > 600) desc = desc[..600];
            }

            company = company.Length > 120 ? company[..120] : company;
            role = role.Length > 120 ? role[..120] : role;
            years = years.Length > 40 ? years[..40] : years;

            toInsert.Add(new WorkExperience
            {
                ProfileId = profileId,
                Company = company,
                Role = role,
                Years = years,
                Description = desc,
                SortOrder = i,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        if (toInsert.Count == 0) return;

        _db.Erfarenheter.AddRange(toInsert);
        await _db.SaveChangesAsync();
    }

    private static bool TryValidateEducationYears(string? educationJson, out string error)
    {
        error = string.Empty;

        var items = DeserializeEducationItems(educationJson);
        for (var i = 0; i < items.Count; i++)
        {
            var years = (items[i].Years ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(years)) continue;

            var normalized = NormalizeYearRange(years);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Utbildning: Ange \"År\" som t.ex. \"2023 - 2025\" eller \"2024 - Pågående\".";
                return false;
            }

            items[i].Years = normalized;
        }

        return true;
    }

    private static bool TryValidateExperienceYears(string? experienceJson, out string error)
    {
        error = string.Empty;

        var items = DeserializeExperienceItems(experienceJson);
        for (var i = 0; i < items.Count; i++)
        {
            var years = (items[i].Years ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(years)) continue;

            var normalized = NormalizeYearRange(years);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                error = "Erfarenhet: Ange \"År\" som t.ex. \"2020 - 2024\" eller \"2022 - Pågående\".";
                return false;
            }

            items[i].Years = normalized;
        }

        return true;
    }

    private static string NormalizeYearRange(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        // Acceptera olika dash-typer och normalisera till '-'
        raw = raw.Replace('–', '-').Replace('—', '-');

        var parts = raw.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return string.Empty;

        var left = parts[0].Trim();
        var right = parts[1].Trim();

        if (!IsYear(left)) return string.Empty;

        if (IsYear(right))
        {
            var y1 = int.Parse(left);
            var y2 = int.Parse(right);
            if (y2 < y1) return string.Empty;
            return $"{y1} - {y2}";
        }

        // Tillåt "Pågående" (case-insensitive)
        if (string.Equals(right, "Pågående", StringComparison.OrdinalIgnoreCase))
        {
            return $"{left} - Pågående";
        }

        return string.Empty;
    }

    private static bool IsYear(string s)
    {
        if (s.Length != 4) return false;
        return int.TryParse(s, out var y) && y is >= 1900 and <= 2100;
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

        // Skapa profil och koppling om den saknas
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

        // Visa-casing: ex. c# => C#, sql => SQL
        items = items.Select(ToSkillDisplay).ToArray();

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
            .Select(s => ToSkillDisplay((s ?? string.Empty).Trim()))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0) return null;

        var sb = new StringBuilder();
        foreach (var n in normalized)
        {
            if (sb.Length > 0) sb.Append(',');
            if (sb.Length + n.Length > 1000) break;
            sb.Append(n);
        }

        return sb.ToString();
    }

    private static string ToSkillDisplay(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        // Normalisera vanliga tech-token till snygg visning
        var t = s.Trim();

        if (string.Equals(t, "c#", StringComparison.OrdinalIgnoreCase)) return "C#";
        if (string.Equals(t, "f#", StringComparison.OrdinalIgnoreCase)) return "F#";
        if (string.Equals(t, "sql", StringComparison.OrdinalIgnoreCase)) return "SQL";
        if (string.Equals(t, "html", StringComparison.OrdinalIgnoreCase)) return "HTML";
        if (string.Equals(t, "css", StringComparison.OrdinalIgnoreCase)) return "CSS";
        if (string.Equals(t, "js", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "javascript", StringComparison.OrdinalIgnoreCase)) return "JavaScript";
        if (string.Equals(t, "ts", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "typescript", StringComparison.OrdinalIgnoreCase)) return "TypeScript";
        if (string.Equals(t, ".net", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "dotnet", StringComparison.OrdinalIgnoreCase)) return ".NET";
        if (string.Equals(t, "asp.net", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "aspnet", StringComparison.OrdinalIgnoreCase)) return "ASP.NET";
        if (string.Equals(t, "mvc", StringComparison.OrdinalIgnoreCase)) return "MVC";
        if (string.Equals(t, "api", StringComparison.OrdinalIgnoreCase)) return "API";
        if (string.Equals(t, "azure", StringComparison.OrdinalIgnoreCase)) return "Azure";
        if (string.Equals(t, "aws", StringComparison.OrdinalIgnoreCase)) return "AWS";

        // Om token är kort och bara bokstäver, gör den versal (t.ex. ui -> UI)
        if (t.Length <= 4 && t.All(char.IsLetter))
        {
            return t.ToUpperInvariant();
        }

        // Annars använd TitleCase-liknande normalisering för läsbarhet
        return NameNormalizer.ToDisplayName(t);
    }

    private async Task<string> SaveAvatarAsync(string userId, IFormFile file)
    {
        // Spara avatar under wwwroot/uploads/avatars/{userId} med kontrollerad extension och filnamn.
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

        // Returnera webbsökväg till filen
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
                join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                from u in users.DefaultIfEmpty()
                where p.CreatedByUserId == userId
                   || _db.ProjektAnvandare.AsNoTracking().Any(pu => pu.ProjectId == p.Id && pu.UserId == userId)
                select new EditCvProjectPickVm
                {
                    Id = p.Id,
                    Title = p.Titel,
                    CreatedUtc = p.CreatedUtc,
                    ImagePath = p.ImagePath,
                    ShortDescription = p.KortBeskrivning,
                    TechKeysCsv = p.TechStackKeysCsv,
                    CreatedByName = u == null ? null : ((u.FirstName + " " + u.LastName).Trim()),
                    CreatedByEmail = u == null ? null : u.Email
                };

        return await q
            .OrderByDescending(x => x.CreatedUtc)
            .ThenBy(x => x.Title)
            .ToListAsync();
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

    private static List<WorkExperienceItemVm> DeserializeExperienceItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();

        try
        {
            return JsonSerializer.Deserialize<List<WorkExperienceItemVm>>(json, JsonOptions) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
