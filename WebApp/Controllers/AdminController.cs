using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Identity;
using WebApp.Infrastructure.Data;

namespace WebApp.Controllers;

[Authorize(Roles = AdminRoleName)]
public sealed class AdminController : Controller
{
    public const string AdminRoleName = "Admin";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Make sure the Admin role exists.
        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(AdminRoleName));
        }

        var users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new ViewModels.AdminIndexVm.UserRow
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                FullName = string.Join(' ', new[] { u.FirstName, u.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                IsDeactivated = u.IsDeactivated
            })
            .ToListAsync();

        // Load admin flags (Identity roles are stored in separate tables)
        var adminIds = new HashSet<string>();
        foreach (var u in users)
        {
            var user = await _userManager.FindByIdAsync(u.Id);
            if (user is null) continue;
            if (await _userManager.IsInRoleAsync(user, AdminRoleName))
            {
                adminIds.Add(u.Id);
            }
        }

        foreach (var u in users)
        {
            u.IsAdmin = adminIds.Contains(u.Id);
        }

        return View("Index", new ViewModels.AdminIndexVm { Users = users });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakeAdmin([FromForm] string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(AdminRoleName));
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        await _userManager.AddToRoleAsync(user, AdminRoleName);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin([FromForm] string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Prevent locking yourself out.
        var meId = _userManager.GetUserId(User);
        if (string.Equals(meId, user.Id, StringComparison.Ordinal))
        {
            TempData["ToastTitle"] = "Inte tillåtet";
            TempData["ToastMessage"] = "Du kan inte ta bort din egen Admin-roll.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.RemoveFromRoleAsync(user, AdminRoleName);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser([FromForm] string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Hard rule: never allow deleting an admin.
        if (await _userManager.IsInRoleAsync(user, AdminRoleName))
        {
            TempData["ToastTitle"] = "Inte tillåtet";
            TempData["ToastMessage"] = "Du kan inte ta bort ett admin-konto.";
            return RedirectToAction(nameof(Index));
        }

        // Best-effort cleanup of app-owned data (messages, profile link, profile, etc.)
        await DeleteUserDataAsync(user.Id);

        var res = await _userManager.DeleteAsync(user);
        if (!res.Succeeded)
        {
            TempData["ToastTitle"] = "Fel";
            TempData["ToastMessage"] = "Kunde inte ta bort användaren.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAllNonAdmins()
    {
        // Deletes all users that are not admins, plus their app-owned data.

        var users = await _db.Users.ToListAsync();
        foreach (var u in users)
        {
            if (await _userManager.IsInRoleAsync(u, AdminRoleName))
            {
                continue;
            }

            await DeleteUserDataAsync(u.Id);
            await _userManager.DeleteAsync(u);
        }

        TempData["ToastTitle"] = "Klart";
        TempData["ToastMessage"] = "Alla icke-admin användare har tagits bort.";

        return RedirectToAction(nameof(Index));
    }

    private async Task DeleteUserDataAsync(string userId)
    {
        // Messages (sent or received)
        var msgs = await _db.UserMessages.Where(m => m.RecipientUserId == userId || m.SenderUserId == userId).ToListAsync();
        if (msgs.Count > 0) _db.UserMessages.RemoveRange(msgs);

        // Project links
        var projUsers = await _db.ProjektAnvandare.Where(x => x.UserId == userId).ToListAsync();
        if (projUsers.Count > 0) _db.ProjektAnvandare.RemoveRange(projUsers);

        // CV profile + link + child tables
        var link = await _db.ApplicationUserProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (link is not null)
        {
            var profileId = link.ProfileId;

            var visits = await _db.ProfilBesok.Where(x => x.VisitorUserId == userId || x.ProfileId == profileId).ToListAsync();
            if (visits.Count > 0) _db.ProfilBesok.RemoveRange(visits);

            var edu = await _db.Utbildningar.Where(x => x.ProfileId == profileId).ToListAsync();
            if (edu.Count > 0) _db.Utbildningar.RemoveRange(edu);

            var exp = await _db.Erfarenheter.Where(x => x.ProfileId == profileId).ToListAsync();
            if (exp.Count > 0) _db.Erfarenheter.RemoveRange(exp);

            var profile = await _db.Profiler.FirstOrDefaultAsync(x => x.Id == profileId);
            _db.ApplicationUserProfiles.Remove(link);
            if (profile is not null) _db.Profiler.Remove(profile);
        }

        // Projects created by user (delete). This may cascade depending on FK setup.
        var projects = await _db.Projekt.Where(p => p.CreatedByUserId == userId).ToListAsync();
        if (projects.Count > 0) _db.Projekt.RemoveRange(projects);

        await _db.SaveChangesAsync();
    }
}
