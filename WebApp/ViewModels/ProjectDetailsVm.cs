namespace WebApp.ViewModels;

public sealed class ProjectDetailsVm
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string Description { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public string? CreatedByName { get; init; }
    public string? CreatedByEmail { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }
    public string? CreatedByUserId { get; init; }

    public string[] TechKeys { get; init; } = Array.Empty<string>();

    public bool IsOwner { get; init; }
    public bool IsMember { get; init; }

    public List<ParticipantVm> Participants { get; init; } = new();

    public sealed class ParticipantVm
    {
        public string UserId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string? Headline { get; init; }
    }
}
