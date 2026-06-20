using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiraSepet.EntityLayer
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal SalePrice { get; set; }

        public int Quantity { get; set; }

        public string UserName { get; set; } = string.Empty;
    }
}
