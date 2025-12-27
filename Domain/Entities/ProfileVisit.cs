namespace WebApp.Domain.Entities;

/// <summary>
/// Loggrad för ett profilbesök, avsedd för statistik och spårning.
/// </summary>
public class ProfileVisit
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    // Nullable för att stödja anonyma besökare (ingen inloggad användare)
    public string? VisitorUserId { get; set; }

    // Besökarens IP-adress sparas som sträng (stöd för IPv4/IPv6)
    public string? VisitorIp { get; set; }

    // UTC-tidsstämpel satt vid instansiering
    public DateTimeOffset VisitedUtc { get; set; } = DateTimeOffset.UtcNow;
}