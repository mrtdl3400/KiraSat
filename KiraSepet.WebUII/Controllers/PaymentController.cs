using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using KiraSepet.WebUI.Controllers;

using KiraSepet.EntityLayer;
namespace KiraSepet.WebUII.Controllers
{
    public class PaymentController : Controller
    {
        private readonly Context _context;

        public PaymentController(Context context)
        {
            _context = context;
        }

        public static List<Order> orders = new List<Order>();

        public IActionResult Index(
    int? productId,
    DateTime? startDate,
    DateTime? endDate,
    string totalPrice)
{
    decimal parsedTotal = 0;

            if (productId != null && startDate != null && endDate != null)
            {
                var product = _context.Products.FirstOrDefault(x => x.Id == productId);

                if (product != null)
                {
                    int totalDays = (endDate.Value - startDate.Value).Days;

                    if (totalDays > 0)
                    {
                        parsedTotal = product.DailPrice.Value * totalDays;
                    }
                }
            }
            else if (productId != null)
            {
                var saleProduct = _context.Products.FirstOrDefault(x => x.Id == productId);

                if (saleProduct != null)
                {
                    parsedTotal = saleProduct.SalePrice;
                }
            }

            ViewBag.ProductId = productId;
    ViewBag.StartDate = startDate;
    ViewBag.EndDate = endDate;
    ViewBag.TotalPrice = parsedTotal;

    HttpContext.Session.SetInt32("RentalProductId", productId ?? 0);
    HttpContext.Session.SetString("RentalStartDate", startDate?.ToString("yyyy-MM-dd") ?? "");
    HttpContext.Session.SetString("RentalEndDate", endDate?.ToString("yyyy-MM-dd") ?? "");
    HttpContext.Session.SetString("RentalTotalPrice", parsedTotal.ToString());
    ViewBag.CartTotal = CartController.cartItems.Sum(x => x.SalePrice * x.Quantity);

            return View();
}

        public IActionResult CompletePayment()
        {
           
        
            var rentalTotal = HttpContext.Session.GetString("RentalTotalPrice");
            var productId = HttpContext.Session.GetInt32("RentalProductId") ?? 0;

            if (!string.IsNullOrEmpty(rentalTotal) && productId != 0)
            {
                var paymentProduct = _context.Products.FirstOrDefault(x => x.Id == productId);

                if (paymentProduct != null)
                {
                    var order = new Order
                    {
                        ProductName = paymentProduct.ProductName,
                        Price = paymentProduct.SalePrice,
                        Quantity = 1,
                        TotalPrice = Convert.ToDecimal(rentalTotal),
                        OrderDate = DateTime.Now,
                        UserEmail = HttpContext.Session.GetString("UserEmail")
                    };

                    _context.Orders.Add(order);

                    paymentProduct.StockCount -= 1;

                    _context.SaveChanges();

                    HttpContext.Session.Remove("RentalProductId");
                    HttpContext.Session.Remove("RentalTotalPrice");

                    TempData["OrderMessage"] = "Ödemeniz gerçekleşti. Siparişiniz oluşturuldu!";

                    return RedirectToAction("Index", "Order");
                }
            }

           

                foreach (var item in CartController.cartItems)
            {
                var order = new Order
                {
                    ProductName = item.ProductName,
                    Price = item.SalePrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.SalePrice * item.Quantity,
                    OrderDate = DateTime.Now,
                    UserEmail = HttpContext.Session.GetString("UserEmail")

                };
                _context.Orders.Add(order);

                // STOK DÜŞÜR
                var soldProduct = _context.Products.FirstOrDefault(x => x.ProductName == item.ProductName);

                if (soldProduct != null && soldProduct.StockCount >= item.Quantity)
                {
                    soldProduct.StockCount -= item.Quantity;
                }

                _context.SaveChanges();
            }

            var rentalProductId = HttpContext.Session.GetInt32("RentalProductId") ?? 0;

            var product = _context.Products.FirstOrDefault(x => x.Id == rentalProductId);

            var rentalStartDateString = HttpContext.Session.GetString("RentalStartDate");
            var rentalEndDateString = HttpContext.Session.GetString("RentalEndDate");

            if (!string.IsNullOrEmpty(rentalStartDateString) && !string.IsNullOrEmpty(rentalEndDateString))
            {
                var startDate = Convert.ToDateTime(rentalStartDateString);
                var endDate = Convert.ToDateTime(rentalEndDateString);
                var totalPrice = Convert.ToDecimal(HttpContext.Session.GetString("RentalTotalPrice"));

                var rentalOrder = new RentalOrder
                {
                    ProductName = product?.ProductName ?? "Ürün Bulunamadı",
                    DailyRentPrice = product?.DailPrice ?? 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDays = (endDate - startDate).Days,
                    TotalPrice = totalPrice,
                    UserEmail = HttpContext.Session.GetString("UserEmail"),
                    OrderDate = DateTime.Now,
                    Status = "Kiralandı",
                    Quantity = 1
                };

                _context.RentalOrders.Add(rentalOrder);

                // KİRALAMA STOK DÜŞÜR
                if (product != null && product.StockCount >= 1)
                {
                    product.StockCount -= 1;
                }

                _context.SaveChanges();
            }



            CartController.cartItems.Clear();
            
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("CartCount");

            TempData["OrderMessage"] = "Ödemeniz gerçekleştirildi. Siparişiniz oluşturuldu!";

            return RedirectToAction("Index", "Order");
        }

    }
}
