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
        // Ürünün ait olduğu işletme. Eski ürünlerde başlangıçta boş olabilir.
        public int? BusinessId { get; set; }

        public Business? Business { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public decimal SalePrice { get; set; }  
        
        public bool IsDeleted { get; set; } 

        public decimal? DailPrice { get; set; }
        public bool IsRentable { get; set; }
        public string? RentType { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }


        public int CategoryId { get; set; } 
        public Category Category { get; set; } = null!;
        public int StockCount { get; set; }
        public string? Description { get; set; }
        public string City { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string? Address { get; set; }
        public decimal? DailyPrice { get; set; }
    }
}
