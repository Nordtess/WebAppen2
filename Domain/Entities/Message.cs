namespace WebApp.Domain.Entities;

/// <summary>
/// Legacy-entitet för bakåtkompatibilitet.
/// Nyare meddelandefunktionalitet använder `Conversation` och `DirectMessage`.
/// </summary>
public class Message
{
    public int Id { get; set; }

    public string? Avsandare { get; set; }

    public string? Text { get; set; }

    // UTC-tidsstämpel satt vid instansiering
    public DateTimeOffset Skickad { get; set; } = DateTimeOffset.UtcNow;
}