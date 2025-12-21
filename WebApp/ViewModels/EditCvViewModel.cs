using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public sealed class EditCvViewModel
{
    // Read-only (from AspNetUsers)
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Location { get; set; } = "";

    // Editable (CV-owned)
    [Display(Name = "Rubrik")]
    [StringLength(60, ErrorMessage = "Rubrik får max vara 60 tecken.")]
    public string? Headline { get; set; }

    [Display(Name = "Om mig")]
    [Required(ErrorMessage = "Om mig är obligatoriskt.")]
    [StringLength(500, ErrorMessage = "Om mig får max vara 500 tecken.")]
    public string AboutMe { get; set; } = "";

    // Skills (edited via JS as JSON array)
    public string SkillsJson { get; set; } = "[]";

    // Existing placeholders: keep as JSON strings until backed by tables.
    public string EducationJson { get; set; } = "[]";
    public string SelectedProjectsJson { get; set; } = "[]";

    // Existing stored image (for preview on GET)
    public string? ProfileImagePath { get; set; }
}
