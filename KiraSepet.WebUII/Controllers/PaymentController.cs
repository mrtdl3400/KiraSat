using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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
    decimal? totalPrice)
        {
            ViewBag.ProductId = productId;

            ViewBag.StartDate = startDate;

            ViewBag.EndDate = endDate;

            ViewBag.TotalPrice = totalPrice ?? 0;

            HttpContext.Session.SetInt32("RentalProductId", productId ?? 0);

            HttpContext.Session.SetString("RentalStartDate",
                startDate?.ToString("yyyy-MM-dd") ?? "");

            HttpContext.Session.SetString("RentalEndDate",
                endDate?.ToString("yyyy-MM-dd") ?? "");

            HttpContext.Session.SetString("RentalTotalPrice",
                (totalPrice ?? 0).ToString());

            return View();
        }


        public IActionResult CompletePayment()
        {
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
                _context.SaveChanges();
            }

            var productId = HttpContext.Session.GetInt32("RentalProductId") ?? 0;

            var product = _context.Products.FirstOrDefault(x => x.Id == productId);

            var startDate = Convert.ToDateTime(HttpContext.Session.GetString("RentalStartDate"));

            var endDate = Convert.ToDateTime(HttpContext.Session.GetString("RentalEndDate"));

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

                Status = "Kiralandı"
            };

            _context.RentalOrders.Add(rentalOrder);

            _context.SaveChanges();

           


            CartController.cartItems.Clear();
            
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("CartCount");

            TempData["OrderMessage"] = "Ödemeniz gerçekleştirildi. Siparişiniz oluşturuldu!";

            return RedirectToAction("Index", "Order");
        }

    }
}
