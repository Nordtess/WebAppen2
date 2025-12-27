namespace WebApp.Domain.Entities;

/// <summary>
/// Arbetslivserfarenhet kopplad till en CV-profil.
/// </summary>
public sealed class WorkExperience
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    // Navigationsproperty; null! används för att undertrycka nullable-varning då EF sätter relationen vid körning
    public Profile Profile { get; set; } = null!;

    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Format: "YYYY - YYYY" eller "YYYY - Pågående"
    public string Years { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Lägre värde visas högre upp i CV
    public int SortOrder { get; set; }

    // UTC-tidsstämplar initierade vid instansiering
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
