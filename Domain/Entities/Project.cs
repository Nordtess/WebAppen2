using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain.Entities;

/// <summary>
/// Projekt som kan kopplas till en användare (t.ex. för att visa i ett CV).
/// </summary>
public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Titel { get; set; } = "";

    [StringLength(140)]
    public string? KortBeskrivning { get; set; }

    [Required]
    [StringLength(500)]
    public string Beskrivning { get; set; } = "";

    /// <summary>
    /// Komma-separerade tech-nycklar som matchar SVG-filer i wwwroot/images/svg/techstack.
    /// Ex: "csharp,mysql,aspnet".
    /// </summary>
    [StringLength(500)]
    public string? TechStackKeysCsv { get; set; }

    // Identity-användarens UserId för den som skapade projektet.
    public string? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}