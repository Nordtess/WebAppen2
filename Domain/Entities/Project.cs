using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp.Domain.Entities;

/// <summary>
/// Projekt som kan kopplas till en användare och visas i t.ex. ett CV.
/// </summary>
public class Project
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Titel { get; set; } = string.Empty;

    [StringLength(140)]
    public string? KortBeskrivning { get; set; }

    [Required]
    [StringLength(500)]
    public string Beskrivning { get; set; } = string.Empty;

    [Column("ProjectImagePath")]
    [StringLength(260)]
    // Relativ sökväg under wwwroot till vald bild, t.ex. "/images/projects/rocketship.png"
    public string? ImagePath { get; set; }

    [StringLength(500)]
    // Komma-separerade nycklar som matchar SVG-filer i wwwroot/images/svg/techstack
    public string? TechStackKeysCsv { get; set; }

    // Identity-användarens id för den som skapade projektet
    public string? CreatedByUserId { get; set; }

    // UTC-tidsstämplar initierade vid instansiering
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}