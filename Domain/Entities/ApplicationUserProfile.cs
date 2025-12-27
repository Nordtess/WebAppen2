namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en koppling mellan en Identity-användare och en domänprofil (CV).
/// Motsvarar vanligtvis en relationsrad mellan tabellerna för AspNetUsers och Profiles.
/// </summary>
public class ApplicationUserProfile
{
    public int Id { get; set; }

    // Identity-användarens Id (AspNetUsers.Id)
    public string UserId { get; set; } = "";

    public int ProfileId { get; set; }

    // Navigationsproperty; null! används för att undertrycka nullable-varning då EF sätter relationen vid körning.
    public Profile Profile { get; set; } = null!;
}
