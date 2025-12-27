using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApp.Infrastructure.Data;
using System.IO;

namespace WebApp.Services;

public sealed class AccountDeletionService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AccountDeletionService> _logger;
    private readonly IWebHostEnvironment _env;

    public AccountDeletionService(ApplicationDbContext db, ILogger<AccountDeletionService> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public record DeleteResult(bool Success, string? ErrorMessage = null);

    public async Task<DeleteResult> DeleteUserAndOwnedDataAsync(string userId)
    {
        var filesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // 1) Profiles (ApplicationUserProfiles -> Profile)
            var profileIds = await _db.ApplicationUserProfiles
                .Where(x => x.UserId == userId)
                .Select(x => x.ProfileId)
                .ToListAsync();

            // Also include profiles that have OwnerUserId set directly
            var ownedProfileIds = await _db.Profiler
                .Where(p => p.OwnerUserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var id in ownedProfileIds)
            {
                if (!profileIds.Contains(id)) profileIds.Add(id);
            }

            profileIds = profileIds.Distinct().ToList();

            if (profileIds.Count > 0)
            {
                var profileImagePaths = await _db.Profiler
                    .Where(p => profileIds.Contains(p.Id))
                    .Select(p => p.ProfileImagePath)
                    .ToListAsync();

                AddUploadFiles(profileImagePaths, filesToDelete);

                await _db.Utbildningar.Where(e => profileIds.Contains(e.ProfileId)).ExecuteDeleteAsync();
                await _db.Erfarenheter.Where(w => profileIds.Contains(w.ProfileId)).ExecuteDeleteAsync();

                await _db.ProfilBesok
                    .Where(v => profileIds.Contains(v.ProfileId) || v.VisitorUserId == userId)
                    .ExecuteDeleteAsync();

                await _db.ApplicationUserProfiles.Where(x => x.UserId == userId).ExecuteDeleteAsync();

                await _db.Profiler.Where(p => profileIds.Contains(p.Id)).ExecuteDeleteAsync();
            }

            // 2) User messages (recipient or sender)
            await _db.UserMessages
                .Where(m => m.RecipientUserId == userId || m.SenderUserId == userId)
                .ExecuteDeleteAsync();

            // 3) Conversations / DirectMessages
            var conversationIds = await _db.ConversationParticipants
                .Where(p => p.UserId == userId)
                .Select(p => p.ConversationId)
                .Distinct()
                .ToListAsync();

            if (conversationIds.Count > 0)
            {
                await _db.DirectMessages
                    .Where(dm => conversationIds.Contains(dm.ConversationId) || dm.SenderUserId == userId)
                    .ExecuteDeleteAsync();

                await _db.ConversationParticipants
                    .Where(p => conversationIds.Contains(p.ConversationId))
                    .ExecuteDeleteAsync();

                await _db.Conversations
                    .Where(c => conversationIds.Contains(c.Id))
                    .ExecuteDeleteAsync();
            }

            // 4) Projects owned by user
            var ownedProjects = await _db.Projekt
                .Where(p => p.CreatedByUserId == userId)
                .Select(p => new { p.Id, p.ImagePath })
                .ToListAsync();

            if (ownedProjects.Count > 0)
            {
                AddUploadFiles(ownedProjects.Select(x => x.ImagePath), filesToDelete);

                var ownedProjectIds = ownedProjects.Select(x => x.Id).ToList();

                // Remove project members
                await _db.ProjektAnvandare.Where(x => ownedProjectIds.Contains(x.ProjectId)).ExecuteDeleteAsync();

                // Remove projects
                await _db.Projekt.Where(p => ownedProjectIds.Contains(p.Id)).ExecuteDeleteAsync();
            }

            // 5) Remove user's membership in other projects
            await _db.ProjektAnvandare.Where(x => x.UserId == userId).ExecuteDeleteAsync();

            // Commit DB
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _logger.LogError(ex, "DeleteUserAndOwnedDataAsync failed for userId={UserId}", userId);
            return new DeleteResult(false, "Radering misslyckades (databasen).");
        }

        // Delete files best-effort
        TryDeleteFiles(filesToDelete);

        return new DeleteResult(true);
    }

    private static void AddUploadFiles(IEnumerable<string?> paths, HashSet<string> target)
    {
        foreach (var p in paths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (p.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) || p.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var web = p.StartsWith("/") ? p : "/" + p;
                target.Add(web);
            }
        }
    }

    private void TryDeleteFiles(HashSet<string> webPaths)
    {
        foreach (var webPath in webPaths)
        {
            try
            {
                var rel = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var abs = Path.Combine(_env.WebRootPath, rel);

                if (File.Exists(abs)) File.Delete(abs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete file for webPath={WebPath}", webPath);
            }
        }
    }
}
