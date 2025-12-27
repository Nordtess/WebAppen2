using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Domain.Identity;

/// <summary>
/// Utökad Identity-användare med extra profilfält som sparas i tabellen AspNetUsers.
/// Innehåller både profilinformation och flaggor för onboarding/status.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
    [StringLength(50, ErrorMessage = "Förnamn får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zåäöÅÄÖ-]+$", ErrorMessage = "Förnamn får bara innehålla bokstäver och bindestreck.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
    [StringLength(50, ErrorMessage = "Efternamn får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zåäöÅÄÖ-]+$", ErrorMessage = "Efternamn får bara innehålla bokstäver och bindestreck.")]
    public string LastName { get; set; } = string.Empty;

    // För snabb och förutsägbar skiftlägesokänslig sökning/filtrering
    public string FirstNameNormalized { get; set; } = string.Empty;

    public string LastNameNormalized { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefonnummer har fel format.")]
    [StringLength(20, ErrorMessage = "Telefonnummer får vara max {1} tecken.")]
    [RegularExpression("^[0-9+\\- ]+$", ErrorMessage = "Telefonnummer får bara innehålla siffror, mellanslag, + och -.")]
    public string? PhoneNumberDisplay { get; set; }

    [Required(ErrorMessage = "Stad är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Stad får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zåäöÅÄÖ -]+$", ErrorMessage = "Stad får bara innehålla bokstäver, mellanslag och bindestreck.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postnummer är obligatoriskt.")]
    [RegularExpression("^[0-9]{5}$", ErrorMessage = "Postnummer måste vara exakt 5 siffror (ex: 71412).")]
    public string PostalCode { get; set; } = string.Empty;

    // Mjuk avaktivering: döljer användare i listor/sök utan att radera data
    public bool IsDeactivated { get; set; }

    // Om profilen ska visas publikt eller vara privat
    public bool IsProfilePrivate { get; set; }

    [StringLength(260)]
    // Relativ sökväg under wwwroot till profilbild
    public string? ProfileImagePath { get; set; }

    // --- Persistenta onboarding-flaggor ---

    // True när användaren sparat konto/profil minst en gång
    public bool HasCompletedAccountProfile { get; set; }

    // True när användaren skapat/sparat CV minst en gång
    public bool HasCreatedCv { get; set; }

    // UTC-tidsstämpel när kontot skapades; används bl.a. för sortering
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
