namespace WebApp.ViewModels;

/// <summary>
/// ViewModel för projektsöksidan med filter- och sorteringsinformation samt listar projektkort.
/// </summary>
public sealed class ProjectsIndexVm
{
    public string Query { get; init; } = string.Empty;

    // Sökomfång: "all" | "title" | "created" | "member"
    public string Scope { get; init; } = "all";

    // Visa endast projekt skapade av aktuell användare
    public bool OnlyMine { get; init; }

    // Sorteringsnyckel, t.ex. "new"
    public string Sort { get; init; } = "new";

    // Filtrera på angiven användares projekt/medlemskap
    public string? FilterUserId { get; init; }

    // Visas som tips när användaren är anonym
    public bool ShowLoginTip { get; init; }

    public List<ProjectCardVm> Projects { get; init; } = new();

    public sealed class ProjectCardVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }

        // Kommaseparerade nycklar för teknologier (valfritt, för presentation/filtrering)
        public string? TechKeysCsv { get; init; }

        // Exponerad bildväg för projektet
        public string? ImagePath { get; init; }

        // Metadata för sökning/visning
        public string? CreatedByName { get; init; }
        public string? CreatedByEmail { get; init; }
    }
}
