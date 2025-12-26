namespace WebApp.ViewModels;

public sealed class AdminIndexVm
{
    public List<UserRow> Users { get; init; } = new();

    public sealed class UserRow
    {
        public string Id { get; init; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsDeactivated { get; set; }
        public bool IsAdmin { get; set; }
    }
}
