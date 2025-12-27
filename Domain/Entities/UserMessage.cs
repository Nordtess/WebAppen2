namespace WebApp.Domain.Entities;

/// <summary>
/// Meddelande i "mail"-stil mellan användare; stödjer både inloggad och anonym avsändare.
/// </summary>
public class UserMessage
{
  public int Id { get; set; }

  // Mottagarens Identity-användar-id (AspNetUsers.Id)
  public string RecipientUserId { get; set; } = string.Empty;

  // Avsändarens Identity-användar-id; null om meddelandet skickats anonymt
  public string? SenderUserId { get; set; }

  // Visningsfält för avsändarens namn (används t.ex. vid anonymt skickade meddelanden)
  public string? SenderName { get; set; }

  public string? SenderEmail { get; set; }

  public string Subject { get; set; } = string.Empty;

  public string Body { get; set; } = string.Empty;

  // Sätts till true när mottagaren har läst meddelandet
  public bool IsRead { get; set; }

  // UTC-tidsstämpel då meddelandet skickades
  public DateTimeOffset SentUtc { get; set; } = DateTimeOffset.UtcNow;

  // UTC-tidsstämpel då meddelandet lästes; null om inte läst
  public DateTimeOffset? ReadUtc { get; set; }
}
