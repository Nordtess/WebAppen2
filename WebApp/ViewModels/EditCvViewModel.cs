using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

/// <summary>
/// Modell för sidan där användaren redigerar sitt CV.
/// Vissa fält hålls som JSON-strängar eftersom klienten hanterar dem via JavaScript.
/// </summary>
public sealed class EditCvViewModel
{
    // Läses från AspNetUsers (read-only i formuläret)
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Location { get; set; } = "";

    [Display(Name = "Rubrik")]
    [Required(ErrorMessage = "Titel är obligatoriskt.")]
    [StringLength(60, ErrorMessage = "Rubrik får max vara 60 tecken.")]
    [RegularExpression("^(?=.*[A-Za-zÅÄÖåäö])[A-Za-zÅÄÖåäö\\- ]+$", ErrorMessage = "Titel måste innehålla minst 1 bokstav och får bara innehålla bokstäver (A-Ö), mellanslag och bindestreck.")]
    public string? Headline { get; set; }


    [Display(Name = "Om mig")]
    [Required(ErrorMessage = "Om mig är obligatoriskt.")]
    [StringLength(500, ErrorMessage = "Om mig får max vara 500 tecken.")]
    public string AboutMe { get; set; } = "";

    // Utbildning och erfarenhet hanteras via JSON i klienten
    public string EducationJson { get; set; } = "[]";

    // Erfarenheter hanteras i tabell men hålls temporärt som JSON i vyn
    public string ExperienceJson { get; set; } = "[]";

    // Valda projekt som JSON-array av id:n (sparas oförändrat vid persistens)
    public string SelectedProjectsJson { get; set; } = "[]";

    // Data för projektväljaren i EditCV
    public int[] SelectedProjectIds { get; set; } = Array.Empty<int>();
    public List<EditCvProjectPickVm> AllMyProjects { get; set; } = new();

    // Sökväg till befintlig profilbild för förhandsgranskning vid GET
    public string? ProfileImagePath { get; set; }
    public string? ExistingProfileImagePath { get; set; }

    // Kompetenser (katalog + val)
    public List<CompetenceGroupVm> CompetenceGroups { get; set; } = new();
    public int[] SelectedCompetenceIds { get; set; } = Array.Empty<int>();
    public string[] SelectedCompetenceNames { get; set; } = Array.Empty<string>();

    public sealed class CompetenceGroupVm
    {
        public string Category { get; set; } = string.Empty;
        public List<CompetenceItemVm> Items { get; set; } = new();
    }

    public sealed class CompetenceItemVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
