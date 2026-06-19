namespace KiraSepet.EntityLayer
{
    public class AppNotification
    {
        public int Id { get; set; }

        // Bildirimin gönderileceği kullanıcı.
        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // Kullanıcı bildirimi açınca true olacak.
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
