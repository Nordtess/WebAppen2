namespace WebApp.Domain.Entities;

/// <summary>
/// Profil/CV som kan visas publikt eller privat och (valfritt) ägas av en Identity-användare.
/// CV-ägda fält bör ligga här snarare än i AspNetUsers.
/// </summary>
public class Profile
{
    public int Id { get; set; }

    // Nullable för att stödja anonyma/demoprofiler
    public string? OwnerUserId { get; set; }

    // Kort rubrik/titel för profilen
    public string? Headline { get; set; }

    // "Om mig" / biografi
    public string? AboutMe { get; set; }

    // Relativ webbsökväg till uppladdad profilbild, t.ex. "/uploads/avatars/{userId}/file.webp"
    public string? ProfileImagePath { get; set; }

    // Kompetenser lagras som kommaseparerad lista; normalisering sker i affärslogik (trim, unika)
    public string? SkillsCsv { get; set; }

    // JSON som innehåller valda projekt-ID:n — placeholder tills en relations-tabell finns
    public string? SelectedProjectsJson { get; set; }

    public bool IsPublic { get; set; } = true;

    // UTC-tidsstämplar för skapande/uppdatering
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}