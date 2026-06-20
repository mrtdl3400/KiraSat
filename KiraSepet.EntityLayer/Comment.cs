namespace KiraSepet.EntityLayer
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string CommentText { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public Product Product { get; set; } = null!;
        public int Rating { get; set; }
    }
}
