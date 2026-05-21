using System.ComponentModel.DataAnnotations;
using System;

namespace KiraSepet.EntityLayer
{
    public class RentalOrder
    {
        [Key]
        public int Id { get; set; }
        public int ProductId { get; set; }
       

        public string ProductName { get; set; }

        public decimal DailyRentPrice { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int TotalDays { get; set; }

        public decimal TotalPrice { get; set; }

        public string UserEmail { get; set; }

        public DateTime OrderDate { get; set; }

        public string Status { get; set; }
        public int Quantity { get; set; }
    }
}
