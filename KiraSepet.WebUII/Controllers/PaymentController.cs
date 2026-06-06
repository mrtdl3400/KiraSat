using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using KiraSepet.WebUI.Controllers;
using KiraSepet.WebUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace KiraSepet.WebUII.Controllers
{
    public class PaymentController : Controller
    {
        private readonly Context _context;

        public PaymentController(Context context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserName"));
        }

        private string GetCartKey()
        {
            return "Cart_" + HttpContext.Session.GetString("UserName");
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(GetCartKey());

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cartItems)
        {
            HttpContext.Session.SetString(GetCartKey(), JsonSerializer.Serialize(cartItems));
        }

        public static List<Order> orders = new List<Order>();

        public IActionResult Index(int? productId, DateTime? startDate, DateTime? endDate, string type, string totalPrice)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            decimal parsedTotal = 0;

            if (!string.IsNullOrEmpty(totalPrice))
            {
                parsedTotal = Convert.ToDecimal(totalPrice);
            }
            else if (productId != null && startDate != null && endDate != null)
            {
                var product = _context.Products.FirstOrDefault(x => x.Id == productId);

                if (product != null)
                {
                    int totalDays = (endDate.Value - startDate.Value).Days;

                    if (totalDays > 0)
                    {
                        parsedTotal = (product.DailPrice ?? 0) * totalDays;
                    }
                }
            }
            else if (productId != null)
            {
                var saleProduct = _context.Products.FirstOrDefault(x => x.Id == productId);

                if (saleProduct != null)
                {
                    if (type == "rent")
                    {
                        parsedTotal = saleProduct.DailPrice ?? 0;
                    }
                    else
                    {
                        parsedTotal = saleProduct.SalePrice;
                    }
                }
            }

            ViewBag.ProductId = productId;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TotalPrice = parsedTotal;

            HttpContext.Session.SetString("RentalTotalPrice", parsedTotal.ToString());
            HttpContext.Session.SetInt32("RentalProductId", productId ?? 0);

            if (type == "rent" && startDate != null && endDate != null)
            {
                HttpContext.Session.SetString("RentalStartDate", startDate.Value.ToString("o"));
                HttpContext.Session.SetString("RentalEndDate", endDate.Value.ToString("o"));
            }

            ViewBag.CartTotal = 0;

            return View();
        }

        public IActionResult CompletePayment()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            var rentalTotal = HttpContext.Session.GetString("RentalTotalPrice");
            var productId = HttpContext.Session.GetInt32("RentalProductId") ?? 0;

            var startDateStr = HttpContext.Session.GetString("RentalStartDate");
            var endDateStr = HttpContext.Session.GetString("RentalEndDate");
            var userEmail = HttpContext.Session.GetString("UserEmail")
                            ?? HttpContext.Session.GetString("UserName")
                            ?? "";

            if (!string.IsNullOrEmpty(rentalTotal) && productId != 0
            && !string.IsNullOrEmpty(startDateStr)
            && !string.IsNullOrEmpty(endDateStr))
            {
                var paymentProduct = _context.Products.FirstOrDefault(x => x.Id == productId);
                if (paymentProduct != null)
                {

                    var rentalOrder = new RentalOrder


                    {
                        ProductId = paymentProduct.Id,
                        ProductName = paymentProduct.ProductName,
                        DailyRentPrice = paymentProduct.DailPrice ?? 0,
                        StartDate = Convert.ToDateTime(HttpContext.Session.GetString("RentalStartDate")),
                        EndDate = Convert.ToDateTime(HttpContext.Session.GetString("RentalEndDate")),
                        TotalDays = (Convert.ToDateTime(HttpContext.Session.GetString("RentalEndDate")) - Convert.ToDateTime(HttpContext.Session.GetString("RentalStartDate"))).Days,
                        TotalPrice = Convert.ToDecimal(rentalTotal),
                        UserEmail = userEmail,
                        OrderDate = DateTime.Now,
                        Status = "Kiralandı",
                        Quantity = 1
                    };

                    _context.RentalOrders.Add(rentalOrder);

                    paymentProduct.StockCount -= 1;

                    _context.SaveChanges();

                    HttpContext.Session.Remove("RentalProductId");
                    HttpContext.Session.Remove("RentalTotalPrice");
                    HttpContext.Session.Remove("RentalStartDate");
                    HttpContext.Session.Remove("RentalEndDate");



                    TempData["OrderMessage"] = "Kiralama işleminiz gerçekleşti.";

                    return RedirectToAction("Index", "Order");
                }
            }

            var userName = HttpContext.Session.GetString("UserName");

            var cartItems = _context.CartItems
                .Where(x => x.UserName == userName)
                .ToList();

            foreach (var item in cartItems)
            {
                var order = new Order
                {
                    ProductName = item.ProductName,
                    Price = item.SalePrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.SalePrice * item.Quantity,
                    OrderDate = DateTime.Now,
                    UserEmail = userEmail

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
                DateTime rentalStartDate = Convert.ToDateTime(rentalStartDateString);
                DateTime rentalEndDate = Convert.ToDateTime(rentalEndDateString);
                var totalPrice = Convert.ToDecimal(HttpContext.Session.GetString("RentalTotalPrice"));

                var rentalOrder = new RentalOrder
                {
                    ProductName = product?.ProductName ?? "Ürün Bulunamadı",
                    DailyRentPrice = product?.DailPrice ?? 0,
                    StartDate = rentalStartDate,
                    EndDate = rentalEndDate,
                    TotalDays = (rentalEndDate - rentalStartDate).Days,
                    TotalPrice = totalPrice,
                    UserEmail = userEmail,
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



           

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();

            TempData["OrderMessage"] = "Ödemeniz gerçekleştirildi. Siparişiniz oluşturuldu!";

            return RedirectToAction("Index", "Order");
        }

    }
}

    
 
