using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Nuvarande lösenord är obligatoriskt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nuvarande lösenord")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Nytt lösenord är obligatoriskt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nytt lösenord")]
    [StringLength(100, ErrorMessage = "Lösenordet måste vara minst {2} och max {1} tecken.", MinimumLength = 6)]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Bekräfta lösenord är obligatoriskt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Bekräfta lösenord")]
    [Compare(nameof(NewPassword), ErrorMessage = "Lösenord och bekräftelse stämmer inte överens.")]
    public string ConfirmPassword { get; set; } = "";
}
