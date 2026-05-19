namespace KiraSepet.EntityLayer
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string UserName { get; set; }

        public string CommentText { get; set; }

        public DateTime CreatedDate { get; set; }

        public Product Product { get; set; }
        public int Rating { get; set; }
    }
}