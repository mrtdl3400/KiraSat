namespace KiraSepet.EntityLayer;

public class SupportMessage
{
    public int Id { get; set; }

    public int SupportConversationId { get; set; }

    public int SenderUserId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; }
}
