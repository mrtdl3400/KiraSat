using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiraSepet.EntityLayer
{
    public class Product
    {
        public int Id { get; set; } 
        public string ProductName { get; set; } 

        public decimal SalePrice { get; set; }  
        
        public bool IsDeleted { get; set; } 

        public decimal DailPrice { get; set; }
        public bool IsRentable { get; set; }
        public string RentType { get; set; }
        public string Brand { get; set; }
        public string? ImageUrl { get; set; }


        public int CategoryId { get; set; } 
        public Category Category { get; set; }
        public int StockCount { get; set; }
        public string Description { get; set; }
        public string City { get; set; }

        public string District { get; set; }

        public string Address { get; set; }
    }
}
