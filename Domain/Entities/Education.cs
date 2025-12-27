using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en utbildningspost kopplad till en profil (CV).
/// Normaliserad lagring av utbildningsinformation istället för att spara som JSON.
/// </summary>
public class Education
{
    public int Id { get; set; }

    public int ProfileId { get; set; }

    // Navigationsproperty; null! används för att undertrycka nullable-varning då EF sätter relationen vid körning.
    public Profile Profile { get; set; } = null!;

    [Required]
    [StringLength(120)]
    public string School { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Program { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    // Valfri fritext för år/period, t.ex. "2024 – Pågående". Kan senare delas upp i From/To.
    public string Years { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Note { get; set; }

    // Lägre värde visas högre upp i CV
    public int SortOrder { get; set; }

    // UTC-tidsstämplar; initieras vid instansiering
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
