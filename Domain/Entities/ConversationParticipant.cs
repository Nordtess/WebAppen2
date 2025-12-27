namespace WebApp.Domain.Entities;

/// <summary>
/// Entitet som representerar en koppling mellan en konversation och en Identity-användare.
/// Motsvarar en relationsrad som binder en konversation till en användare.
/// </summary>
public class ConversationParticipant
{
    public int Id { get; set; }

    public int ConversationId { get; set; }

    // Identity-användarens Id (AspNetUsers.Id)
    public string UserId { get; set; } = string.Empty;
}
