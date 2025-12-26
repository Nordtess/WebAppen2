namespace WebApp.ViewModels;

public sealed class MessagesIndexVm
{
    public required string Sort { get; init; }
    public required string Query { get; init; }
    public required bool UnreadOnly { get; init; }

    public required int UnreadCount { get; init; }
    public required List<MessageCardVm> Messages { get; init; }

    public sealed class MessageCardVm
    {
        public required int Id { get; init; }
        public required bool IsRead { get; init; }
        public required string FromDisplayName { get; init; }
        public string? FromUserId { get; init; }
        public required DateTimeOffset SentUtc { get; init; }
        public required string Body { get; init; }
        public required string Preview { get; init; }
    }
}
