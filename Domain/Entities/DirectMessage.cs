namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet för ett enskilt direktmeddelande i en konversation.
/// Innehåller avsändare, meddelandets innehåll och UTC-tidsstämpel.
/// </summary>
public class DirectMessage
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    // Avsändarens Identity-användar-id (AspNetUsers.Id)
    public string SenderUserId { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    // UTC-tid satt vid instansiering för att bevara en tidszonsoberoende tidpunkt
    public DateTimeOffset SentUtc { get; set; } = DateTimeOffset.UtcNow;
}
