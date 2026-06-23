namespace KiraSepet.EntityLayer;

public class SupportConversation
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
}
