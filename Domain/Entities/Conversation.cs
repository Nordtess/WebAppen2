namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en konversation/tråd och dess skapandetid.
/// </summary>
public class Conversation
{
    public int Id { get; set; }

    // UTC-tidsstämpel satt vid instansiering; DateTimeOffset bevarar tidszonsinformation.
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
