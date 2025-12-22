namespace WebApp.ViewModels;

public sealed class ProjectsIndexVm
{
    public string Query { get; init; } = string.Empty;
    public string Sort { get; init; } = "new";

    // Optional hint shown when user is anonymous.
    public bool ShowLoginTip { get; init; }

    public List<ProjectCardVm> Projects { get; init; } = new();

    public sealed class ProjectCardVm
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? ShortDescription { get; init; }
        public DateTimeOffset CreatedUtc { get; init; }
        public string? TechKeysCsv { get; init; }

        // For searching/filtering (and later display if we want)
        public string? CreatedByName { get; init; }
        public string? CreatedByEmail { get; init; }
    }
}
