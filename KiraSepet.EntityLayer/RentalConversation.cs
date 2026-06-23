namespace KiraSepet.EntityLayer;

public class RentalConversation
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int TenantUserId { get; set; }

    public int OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
}
