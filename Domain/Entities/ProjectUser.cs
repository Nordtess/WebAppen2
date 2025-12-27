namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en koppling mellan ett projekt och en Identity-användare.
/// </summary>
public class ProjectUser
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    // Identity-användarens id (AspNetUsers.Id)
    public string UserId { get; set; } = string.Empty;

    // UTC-tidsstämpel som anger när kopplingen skapades
    public DateTimeOffset ConnectedUtc { get; set; } = DateTimeOffset.UtcNow;
}