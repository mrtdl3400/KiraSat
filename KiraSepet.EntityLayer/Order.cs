namespace KiraSepet.EntityLayer
{
    public class Order
    {
        public int Id { get; set; }

        public string UserEmail { get; set; }

        public string ProductName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime OrderDate { get; set; }
    }
}
