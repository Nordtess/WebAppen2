using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WebApp.Domain.Identity;

/// <summary>
/// Utökad Identity-användare med extra profilfält som sparas i tabellen AspNetUsers.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Förnamn är obligatoriskt.")]
    [StringLength(50, ErrorMessage = "Förnamn får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zÅÄÖåäö-]+$", ErrorMessage = "Förnamn får bara innehålla bokstäver och bindestreck.")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Efternamn är obligatoriskt.")]
    [StringLength(50, ErrorMessage = "Efternamn får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zÅÄÖåäö-]+$", ErrorMessage = "Efternamn får bara innehålla bokstäver och bindestreck.")]
    public string LastName { get; set; } = "";

    // För snabb och förutsägbar skiftlägesokänslig sökning/filtrering.
    public string FirstNameNormalized { get; set; } = "";

    public string LastNameNormalized { get; set; } = "";

    [Phone(ErrorMessage = "Telefonnummer har fel format.")]
    [StringLength(20, ErrorMessage = "Telefonnummer får vara max {1} tecken.")]
    [RegularExpression("^[0-9+\\- ]+$", ErrorMessage = "Telefonnummer får bara innehålla siffror, mellanslag, + och -.")]
    public string? PhoneNumberDisplay { get; set; }

    [Required(ErrorMessage = "Stad är obligatoriskt.")]
    [StringLength(100, ErrorMessage = "Stad får vara max {1} tecken.")]
    [RegularExpression("^[A-Za-zÅÄÖåäö -]+$", ErrorMessage = "Stad får bara innehålla bokstäver, mellanslag och bindestreck.")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Postnummer är obligatoriskt.")]
    [RegularExpression("^[0-9]{5}$", ErrorMessage = "Postnummer måste vara exakt 5 siffror (ex: 71412).")]
    public string PostalCode { get; set; } = "";

    // Mjuk avaktivering: används för att kunna dölja användare i listor/sök utan att radera data.
    public bool IsDeactivated { get; set; }
}
