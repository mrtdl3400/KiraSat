using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using KiraSepet.WebUI.Controllers;
using KiraSepet.WebUI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                        parsedTotal = (product.DailyPrice ?? 0) * totalDays;
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
                        parsedTotal = saleProduct.DailyPrice ?? 0;
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
            ViewBag.PaymentTermsTitle = GetPaymentTermsTitle();
            ViewBag.PaymentTermsContent = GetPaymentTermsContent();

            HttpContext.Session.Remove("RentalProductId");
            HttpContext.Session.Remove("RentalTotalPrice");
            HttpContext.Session.Remove("RentalStartDate");
            HttpContext.Session.Remove("RentalEndDate");
            HttpContext.Session.Remove("SaleProductId");
            HttpContext.Session.Remove("SaleTotalPrice");
            HttpContext.Session.Remove("PaymentType");

            if (type == "rent" && startDate != null && endDate != null)
            {
                HttpContext.Session.SetString("PaymentType", "rent");
                HttpContext.Session.SetString("RentalTotalPrice", parsedTotal.ToString());
                HttpContext.Session.SetInt32("RentalProductId", productId ?? 0);
                HttpContext.Session.SetString("RentalStartDate", startDate.Value.ToString("o"));
                HttpContext.Session.SetString("RentalEndDate", endDate.Value.ToString("o"));
            }
            else if (type == "buy" && productId != null)
            {
                HttpContext.Session.SetString("PaymentType", "buy");
                HttpContext.Session.SetInt32("SaleProductId", productId.Value);
                HttpContext.Session.SetString("SaleTotalPrice", parsedTotal.ToString());
            }

            ViewBag.CartTotal = 0;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompletePayment(bool acceptedTerms)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Index", "Login");
            }

            if (!acceptedTerms)
            {
                TempData["PaymentError"] = "İşleme devam etmek için kiralama/satın alma şartlarını kabul etmelisiniz.";
                return RedirectToAction("Index");
            }

            var rentalTotal = HttpContext.Session.GetString("RentalTotalPrice");
            var productId = HttpContext.Session.GetInt32("RentalProductId") ?? 0;
            var paymentType = HttpContext.Session.GetString("PaymentType");
            var saleProductId = HttpContext.Session.GetInt32("SaleProductId") ?? 0;

            var startDateStr = HttpContext.Session.GetString("RentalStartDate");
            var endDateStr = HttpContext.Session.GetString("RentalEndDate");
            var userEmail = HttpContext.Session.GetString("UserEmail")
                            ?? HttpContext.Session.GetString("UserName")
                            ?? "";


            if (paymentType == "rent" &&
    productId != 0 &&
    DateTime.TryParse(startDateStr, out var startDate) &&
    DateTime.TryParse(endDateStr, out var endDate))
            {
                if (endDate <= startDate)
                {
                    TempData["OrderError"] = "Bitiş tarihi başlangıç tarihinden sonra olmalı.";
                    return RedirectToAction("ProductDetails", "Product", new { id = productId });
                }

                var paymentProduct = _context.Products.FirstOrDefault(x =>
                    x.Id == productId &&
                    !x.IsDeleted &&
                    x.IsRentable);

                if (paymentProduct == null || paymentProduct.RentalStockCount < 1)
                {
                    TempData["OrderError"] = "Bu ürün şu anda kiralamaya uygun değil.";
                    return RedirectToAction("ProductDetails", "Product", new { id = productId });
                }

                var rentedQuantityForDates = _context.RentalOrders
                    .Where(x =>
                    x.ProductId == productId &&
                    x.Status != "Reddedildi" &&
                    x.StartDate < endDate &&
                        startDate < x.EndDate)
                    .Sum(x => x.Quantity);

                if (rentedQuantityForDates + 1 > paymentProduct.RentalStockCount)
                {
                    TempData["OrderError"] =
                        "Seçtiğiniz tarihler için ürünün kiralama stoğu dolu.";

                    return RedirectToAction("ProductDetails", "Product", new { id = productId });
                }

                var totalDays = (endDate - startDate).Days;
                var totalPrice = (paymentProduct.DailyPrice ?? 0) * totalDays;

                var business = paymentProduct.BusinessId.HasValue
                    ? _context.Businesses.FirstOrDefault(x => x.Id == paymentProduct.BusinessId.Value)
                    : null;

                if (business == null)
                {
                    TempData["OrderError"] = "Bu ürün bir satıcı işletmesine bağlı olmadığı için kiralama talebi gönderilemedi.";
                    return RedirectToAction("ProductDetails", "Product", new { id = productId });
                }

                var rentalOrder = new RentalOrder
                {
                    ProductId = paymentProduct.Id,
                    ProductName = paymentProduct.ProductName,
                    DailyRentPrice = paymentProduct.DailyPrice ?? 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalDays = totalDays,
                    TotalPrice = totalPrice,
                    UserEmail = userEmail,
                    OrderDate = DateTime.Now,
                    Status = "Satıcı Onayı Bekleniyor",
                    Quantity = 1
                };
                _context.RentalOrders.Add(rentalOrder);

                _context.AppNotifications.Add(new AppNotification
                {
                    UserId = business.OwnerUserId,
                    Title = "Yeni kiralama talebi",
                    Message = $"{paymentProduct.ProductName} için {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy} tarihleri arasında yeni bir kiralama talebi var. Onaylamak veya reddetmek için kiralama taleplerini kontrol edin.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                _context.SaveChanges();

                HttpContext.Session.Remove("RentalProductId");
                HttpContext.Session.Remove("RentalTotalPrice");
                HttpContext.Session.Remove("RentalStartDate");
                HttpContext.Session.Remove("RentalEndDate");
                HttpContext.Session.Remove("PaymentType");

                TempData["OrderMessage"] =
                    "Kiralama talebiniz satıcının onayına gönderildi. Satıcı onayladığında sipariş durumunuz güncellenecek.";

                return RedirectToAction("Index", "Order");
            }

            if (paymentType == "buy" && saleProductId != 0)
            {
                var saleProduct = _context.Products.FirstOrDefault(x => x.Id == saleProductId && !x.IsDeleted);

                if (saleProduct == null || saleProduct.StockCount < 1)
                {
                    TempData["OrderError"] = "Bu ürün şu anda satın almaya uygun değil.";
                    return RedirectToAction("Index", "Product");
                }

                _context.Orders.Add(new Order
                {
                    ProductName = saleProduct.ProductName,
                    Price = saleProduct.SalePrice,
                    Quantity = 1,
                    TotalPrice = saleProduct.SalePrice,
                    OrderDate = DateTime.Now,
                    UserEmail = userEmail
                });

                saleProduct.StockCount -= 1;

                if (saleProduct.BusinessId.HasValue)
                {
                    var business = _context.Businesses.FirstOrDefault(x => x.Id == saleProduct.BusinessId.Value);

                    if (business != null)
                    {
                        _context.AppNotifications.Add(new AppNotification
                        {
                            UserId = business.OwnerUserId,
                            Title = "Yeni satış",
                            Message = $"{saleProduct.ProductName} ürünü satın alındı. Siparişler bölümünden satışınızı takip edebilirsiniz.",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                _context.SaveChanges();

                HttpContext.Session.Remove("SaleProductId");
                HttpContext.Session.Remove("SaleTotalPrice");
                HttpContext.Session.Remove("PaymentType");

                TempData["OrderMessage"] = "Satın alma işleminiz başarıyla gerçekleştirildi.";
                return RedirectToAction("Index", "Order");
            }

            var userName = HttpContext.Session.GetString("UserName");

            var cartItems = _context.CartItems
                .Where(x => x.UserName == userName)
                .ToList();

            if (!cartItems.Any())
            {
                TempData["OrderError"] = "Sepetiniz boş.";
                return RedirectToAction("Index", "Cart");
            }

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                foreach (var item in cartItems)
                {
                    var product = _context.Products
                        .FirstOrDefault(x => x.Id == item.ProductId && !x.IsDeleted);

                    if (product == null || product.StockCount < item.Quantity)
                    {
                        transaction.Rollback();
                        TempData["OrderError"] = $"{item.ProductName} için yeterli stok bulunmuyor.";
                        return RedirectToAction("Index", "Cart");
                    }
                }

                foreach (var item in cartItems)
                {
                    var product = _context.Products.First(x => x.Id == item.ProductId);

                    _context.Orders.Add(new Order
                    {
                        ProductName = product.ProductName,
                        Price = product.SalePrice,
                        Quantity = item.Quantity,
                        TotalPrice = product.SalePrice * item.Quantity,
                        OrderDate = DateTime.Now,
                        UserEmail = userEmail
                    });

                    product.StockCount -= item.Quantity;
                }

                _context.CartItems.RemoveRange(cartItems);
                _context.SaveChanges();
                transaction.Commit();

                TempData["OrderMessage"] = "Ödemeniz gerçekleştirildi. Siparişiniz oluşturuldu!";
                return RedirectToAction("Index", "Order");
            }
            catch
            {
                transaction.Rollback();
                TempData["OrderError"] = "Ödeme sırasında bir hata oluştu. İşlem tamamlanmadı.";
                return RedirectToAction("Index", "Cart");
            }
        }

        private string GetPaymentTermsTitle()
        {
            return _context.LegalTexts
                .Where(x => x.Key == "PaymentTerms")
                .Select(x => x.Title)
                .FirstOrDefault() ?? "Kiralama ve satın alma bilgilendirme metni";
        }

        private string GetPaymentTermsContent()
        {
            return _context.LegalTexts
                .Where(x => x.Key == "PaymentTerms")
                .Select(x => x.Content)
                .FirstOrDefault() ?? @"KiraSat üzerinden yapılan satın alma ve kiralama işlemlerinde kullanıcı, seçtiği ürünü açıklamada belirtilen niteliklere göre incelediğini ve işlem bedelini onayladığını kabul eder.

Kiralama işlemlerinde kullanıcı, ürünü teslim aldığı haliyle korumakla, kullanım süresi boyunca ürüne makul özeni göstermekle ve ürünü kararlaştırılan tarihte eksiksiz şekilde iade etmekle sorumludur.

Ürünün teslim alınması sırasında kullanıcı, üründe görünür bir hasar veya eksiklik olup olmadığını kontrol etmelidir. Teslim sonrası ortaya çıkan hasar, kayıp, eksik parça veya geç iade durumlarında satıcı ile kullanıcı arasında sorumluluk değerlendirmesi yapılabilir.

Satıcı, ilana eklediği ürün bilgilerinin doğru olduğunu, ürünün kullanıma uygun durumda bulunduğunu ve teslim sürecinde kullanıcıyı yanıltıcı bilgi vermeyeceğini kabul eder.

Platform, kullanıcı ile satıcı arasındaki alışveriş ve kiralama sürecini kolaylaştıran aracı bir hizmet sunar. Taraflar, işlemle ilgili mesajlaşma ve bildirimleri takip etmekle sorumludur.";
        }

    }
}

    
 
