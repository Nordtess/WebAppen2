using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public sealed class EditCvViewModel
{
    [Display(Name = "Namn")]
    [Required(ErrorMessage = "Namn är obligatoriskt.")]
    [StringLength(60, ErrorMessage = "Namn får max vara 60 tecken.")]
    public string FullName { get; set; } = "";

    [Display(Name = "Titel")]
    [StringLength(60, ErrorMessage = "Titel får max vara 60 tecken.")]
    public string? Headline { get; set; }

    [Display(Name = "Om mig")]
    [Required(ErrorMessage = "Om mig är obligatoriskt.")]
    [StringLength(500, ErrorMessage = "Om mig får max vara 500 tecken.")]
    public string AboutMe { get; set; } = "";

    [Display(Name = "E-post")]
    [Required(ErrorMessage = "E-post är obligatoriskt.")]
    [EmailAddress(ErrorMessage = "Ange en giltig e-postadress.")]
    public string Email { get; set; } = "";

    [Display(Name = "Telefon")]
    [StringLength(30, ErrorMessage = "Telefon får max vara 30 tecken.")]
    [RegularExpression(@"^[0-9+() \-]{6,30}$", ErrorMessage = "Ange ett giltigt telefonnummer.")]
    public string? Phone { get; set; }

    [Display(Name = "Plats")]
    [StringLength(60, ErrorMessage = "Plats får max vara 60 tecken.")]
    public string? Location { get; set; }

    [Display(Name = "LinkedIn")]
    [StringLength(120, ErrorMessage = "LinkedIn får max vara 120 tecken.")]
    // Tillåter tomt, eller linkedin.com/... med/utan https:// och www.
    [RegularExpression(
        @"^$|^(https?:\/\/)?(www\.)?linkedin\.com\/.+$",
        ErrorMessage = "Ange en giltig LinkedIn-länk (t.ex. linkedin.com/in/dittnamn).")]
    public string? LinkedIn { get; set; }

    // (Legacy/placeholder om ni just nu bara vill ha “1 utbildning” i modellen)
    [Display(Name = "Skola")]
    [StringLength(80, ErrorMessage = "Skola får max vara 80 tecken.")]
    public string? EducationSchool { get; set; }

    [Display(Name = "Program / inriktning")]
    [StringLength(80, ErrorMessage = "Program / inriktning får max vara 80 tecken.")]
    public string? EducationProgram { get; set; }

    [Display(Name = "Startår")]
    [StringLength(10, ErrorMessage = "Startår får max vara 10 tecken.")]
    public string? EducationFrom { get; set; }

    [Display(Name = "Slutår")]
    [StringLength(10, ErrorMessage = "Slutår får max vara 10 tecken.")]
    public string? EducationTo { get; set; }

    // (Legacy/placeholder om ni har “1 erfarenhet” i modellen)
    [Display(Name = "Arbetsplats")]
    [StringLength(80, ErrorMessage = "Arbetsplats får max vara 80 tecken.")]
    public string? ExperienceCompany { get; set; }

    [Display(Name = "Titel (erfarenhet)")]
    [StringLength(80, ErrorMessage = "Titel får max vara 80 tecken.")]
    public string? ExperienceTitle { get; set; }

    [Display(Name = "Period")]
    [StringLength(30, ErrorMessage = "Period får max vara 30 tecken.")]
    public string? ExperiencePeriod { get; set; }

    [Display(Name = "Beskrivning")]
    [StringLength(500, ErrorMessage = "Beskrivning får max vara 500 tecken.")]
    public string? ExperienceDescription { get; set; }
}
