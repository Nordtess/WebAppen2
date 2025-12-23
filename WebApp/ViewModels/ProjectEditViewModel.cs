using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels;

public sealed class ProjectEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Titel är obligatoriskt.")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "Titel måste vara mellan {2} och {1} tecken.")]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Kort beskrivning är obligatoriskt.")]
    [StringLength(140, ErrorMessage = "Kort beskrivning får max vara 140 tecken.")]
    public string ShortDescription { get; set; } = "";

    [Required(ErrorMessage = "Beskrivning är obligatoriskt.")]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Beskrivning får max vara {1} tecken.")]
    public string Description { get; set; } = "";

    // Tech selection (keys like "csharp", "mysql" etc)
    public string TechStackJson { get; set; } = "[]";

    // Selected image path in wwwroot (ex: /images/projects/rocketship.png)
    public string? ProjectImage { get; set; }

    // Read-only meta
    public string CreatedText { get; set; } = "";
    public bool IsOwner { get; set; }
}
