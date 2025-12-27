using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure.Data;
using WebApp.ViewModels;

namespace WebApp.Controllers;

[Authorize]
public sealed class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;

    public MessagesController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Visar inkorgen för inloggad användare med filtrering, sökning och sortering.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? sort, [FromQuery] string? q, [FromQuery] string? unreadOnly)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        // Normalisera sorteringsläge
        var sortMode = (sort ?? "new").Trim().ToLowerInvariant();
        if (sortMode is not ("new" or "old")) sortMode = "new";

        // Tolkning av flagga för endast olästa
        var onlyUnread = string.Equals((unreadOnly ?? "0").Trim(), "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals((unreadOnly ?? "").Trim(), "true", StringComparison.OrdinalIgnoreCase);

        var query = (q ?? string.Empty).Trim();
        var qNorm = query.ToUpperInvariant();

        // Basfråga: användarens mottagna meddelanden (AsNoTracking för läsning)
        var baseQuery = _db.UserMessages.AsNoTracking()
            .Where(m => m.RecipientUserId == userId);

        if (onlyUnread)
        {
            baseQuery = baseQuery.Where(m => !m.IsRead);
        }

        // Join mot Users för att kunna visa och söka på avsändarens kontoinformation
        var joined = from m in baseQuery
                     join su in _db.Users.AsNoTracking() on m.SenderUserId equals su.Id into sus
                     from su in sus.DefaultIfEmpty()
                     select new { m, su };

        if (!string.IsNullOrWhiteSpace(qNorm))
        {
            joined = joined.Where(x =>
                (x.su != null &&
                 ((x.su.FirstNameNormalized + " " + x.su.LastNameNormalized).Contains(qNorm) ||
                  x.su.FirstNameNormalized.Contains(qNorm) ||
                  x.su.LastNameNormalized.Contains(qNorm)))
                || (!string.IsNullOrWhiteSpace(x.m.SenderName) && x.m.SenderName.ToUpper().Contains(qNorm)));
        }

        joined = sortMode == "old"
            ? joined.OrderBy(x => x.m.SentUtc)
            : joined.OrderByDescending(x => x.m.SentUtc);

        var rows = await joined.Take(200).ToListAsync();

        // Hjälpmetod: skapa förhandsvisning av meddelandetext
        static string MakePreview(string body)
        {
            var t = (body ?? string.Empty).Trim();
            if (t.Length <= 110) return t;
            return t.Substring(0, 110) + "…";
        }

        // Hjälpmetod: bygg visningsnamn från för- och efternamn
        static string DisplayName(string? a, string? b)
        {
            var s = string.Join(' ', new[] { a, b }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.IsNullOrWhiteSpace(s) ? "Okänd" : s;
        }

        // Räkna olästa meddelanden
        var unreadCount = await _db.UserMessages.AsNoTracking()
            .Where(m => m.RecipientUserId == userId && !m.IsRead)
            .CountAsync();

        var vm = new MessagesIndexVm
        {
            Sort = sortMode,
            Query = query,
            UnreadOnly = onlyUnread,
            UnreadCount = unreadCount,
            Messages = rows.Select(x => new MessagesIndexVm.MessageCardVm
            {
                Id = x.m.Id,
                IsRead = x.m.IsRead,
                FromUserId = x.m.SenderUserId,
                FromDisplayName = x.su != null
                    ? DisplayName(x.su.FirstName, x.su.LastName)
                    : (string.IsNullOrWhiteSpace(x.m.SenderName) ? "Okänd" : x.m.SenderName!),
                SentUtc = x.m.SentUtc,
                Body = x.m.Body,
                Preview = MakePreview(x.m.Body)
            }).ToList()
        };

        return View("Messages", vm);
    }

    public sealed class SetReadInput
    {
        public int Id { get; init; }
        public bool IsRead { get; init; }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRead([FromForm] SetReadInput input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var msg = await _db.UserMessages.FirstOrDefaultAsync(m => m.Id == input.Id && m.RecipientUserId == userId);
        if (msg is null) return NotFound();

        msg.IsRead = input.IsRead;
        msg.ReadUtc = input.IsRead ? DateTimeOffset.UtcNow : null;

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var unread = await _db.UserMessages
            .Where(m => m.RecipientUserId == userId && !m.IsRead)
            .ToListAsync();

        if (unread.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var m in unread)
            {
                m.IsRead = true;
                m.ReadUtc = now;
            }

            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var msg = await _db.UserMessages.FirstOrDefaultAsync(m => m.Id == id && m.RecipientUserId == userId);
        if (msg is null) return NotFound();

        _db.UserMessages.Remove(msg);
        await _db.SaveChangesAsync();

        TempData["ToastTitle"] = "Borttaget";
        TempData["ToastMessage"] = "Meddelandet har tagits bort.";

        return RedirectToAction(nameof(Index));
    }

    public sealed class ReplyInput
    {
        public int OriginalMessageId { get; init; }
        public string Body { get; init; } = string.Empty;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply([FromForm] ReplyInput input)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Forbid();

        var original = await _db.UserMessages.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == input.OriginalMessageId && m.RecipientUserId == userId);

        if (original is null) return NotFound();

        // Kan endast svara om ursprungsavsändaren är en autentiserad användare
        if (string.IsNullOrWhiteSpace(original.SenderUserId))
        {
            TempData["ToastTitle"] = "Kan inte svara";
            TempData["ToastMessage"] = "Det här meddelandet skickades anonymt. Du kan inte svara från meddelandesidan.";
            return RedirectToAction(nameof(Index));
        }

        var body = (input.Body ?? string.Empty).Trim();
        if (body.Length is < 1 or > 500)
        {
            TempData["ToastTitle"] = "Ogiltigt";
            TempData["ToastMessage"] = "Svar måste vara 1–500 tecken.";
            return RedirectToAction(nameof(Index));
        }

        var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        var senderName = sender is null
            ? ""
            : string.Join(' ', new[] { sender.FirstName, sender.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        _db.UserMessages.Add(new UserMessage
        {
            RecipientUserId = original.SenderUserId,
            SenderUserId = userId,
            SenderName = string.IsNullOrWhiteSpace(senderName) ? sender?.Email : senderName,
            Subject = string.Empty,
            Body = body,
            SentUtc = DateTimeOffset.UtcNow,
            IsRead = false
        });

        await _db.SaveChangesAsync();

        TempData["ToastTitle"] = "Skickat";
        TempData["ToastMessage"] = "Ditt svar har skickats.";

        return RedirectToAction(nameof(Index));
    }
}
