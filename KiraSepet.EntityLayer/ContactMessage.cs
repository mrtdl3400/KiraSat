namespace KiraSepet.EntityLayer;

public class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? AdminReply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string? UserReply { get; set; }
    public DateTime? UserRepliedAt { get; set; }
}
