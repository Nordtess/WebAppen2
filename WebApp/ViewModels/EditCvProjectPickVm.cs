namespace WebApp.ViewModels;

public sealed class EditCvProjectPickVm
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }
}
