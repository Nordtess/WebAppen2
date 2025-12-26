namespace WebApp.ViewModels;

public sealed class EditCvProjectPickVm
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }

    public string? ImagePath { get; init; }
    public string? ShortDescription { get; init; }
    public string? TechKeysCsv { get; init; }

    public string? CreatedByName { get; init; }
    public string? CreatedByEmail { get; init; }
}
