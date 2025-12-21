namespace WebApp.Domain.Entities;

/// <summary>
/// Profil/CV som kan visas publikt eller privat och (valfritt) ägas av en Identity-användare.
/// CV-ägda fält ska ligga här (inte i AspNetUsers).
/// </summary>
public class Profile
{
    public int Id { get; set; }

    // Nullable för att kunna stödja anonyma/demoprofiler.
    public string? OwnerUserId { get; set; }

    /// <summary>
    /// Kort rubrik/titel för profilen.
    /// </summary>
    public string? Headline { get; set; }

    /// <summary>
    /// "Om mig" / bio.
    /// </summary>
    public string? AboutMe { get; set; }

    /// <summary>
    /// Relativ webbsökväg till uppladdad profilbild (t.ex. "/uploads/avatars/{userId}/file.webp").
    /// </summary>
    public string? ProfileImagePath { get; set; }

    /// <summary>
    /// Kompetenser lagras som kommaseparerad lista (prototyp). Normaliseras (trim, unika) i logik.
    /// </summary>
    public string? SkillsCsv { get; set; }

    /// <summary>
    /// JSON för utbildningar (prototyp/placeholder tills egna tabeller finns).
    /// </summary>
    public string? EducationJson { get; set; }

    /// <summary>
    /// JSON för valda projekt-ID:n (prototyp/placeholder tills kopplingstabell finns).
    /// </summary>
    public string? SelectedProjectsJson { get; set; }

    public bool IsPublic { get; set; } = true;

    // Tidsstämplar i UTC för skapande/uppdatering.
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}