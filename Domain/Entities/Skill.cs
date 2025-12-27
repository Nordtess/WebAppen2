namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en kompetens/skill som kan kopplas till en profil eller användare.
/// </summary>
public class Skill
{
    public int Id { get; set; }

    // Namnet på kompetensen; nullable för att stödja partiella eller inkompletta poster
    public string? Namn { get; set; }
}