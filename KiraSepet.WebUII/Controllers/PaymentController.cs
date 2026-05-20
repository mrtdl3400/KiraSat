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

        public IActionResult Index(decimal? totalPrice)
        {
            ViewBag.TotalPrice = totalPrice ?? 0;
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


            CartController.cartItems.Clear();
            
            HttpContext.Session.Remove("Cart");
            HttpContext.Session.Remove("CartCount");

            TempData["OrderMessage"] = "Ödemeniz gerçekleştirildi. Siparişiniz oluşturuldu!";

            return RedirectToAction("Index", "Cart");
        }

    }
}
