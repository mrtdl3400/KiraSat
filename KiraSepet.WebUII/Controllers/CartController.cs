using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUI.Controllers
{
    public class CartController : Controller
    {
        private readonly Context _context;

        public CartController(Context context)
        {
            _context = context;


        }

        

        private void UpdateCartCount()
        {
            var userName = HttpContext.Session.GetString("UserName");

            var cartCount = _context.CartItems
                .Where(x => x.UserName == userName)
                .Sum(x => x.Quantity);

            HttpContext.Session.SetInt32("CartCount", cartCount);
        }


        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Index", "Login");
            }

            UpdateCartCount();

            var cartItems = _context.CartItems
                .Where(x => x.UserName == userName)
                .ToList();

            return View(cartItems);
        
        }


        public IActionResult AddToCart(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");

            if (userName == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var product = _context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }

            var cartItem = _context.CartItems
                .FirstOrDefault(x => x.ProductId == id && x.UserName == userName);

            if (cartItem == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    SalePrice = product.SalePrice,
                    Quantity = 1,
                    UserName = userName
                });
            }
            else
            {
                cartItem.Quantity++;
            }

            _context.SaveChanges();

            var cartCount = _context.CartItems
                .Where(x => x.UserName == userName)
                .Sum(x => x.Quantity);

            HttpContext.Session.SetInt32("CartCount", cartCount);

            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");

            var cartItem = _context.CartItems
                .FirstOrDefault(x => x.ProductId == id && x.UserName == userName);

            if (cartItem != null)
            {
                cartItem.Quantity++;
                _context.SaveChanges();

                UpdateCartCount();
            }

            return RedirectToAction("Index");
        }


        public IActionResult DecreaseQuantity(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");

            var cartItem = _context.CartItems
                .FirstOrDefault(x => x.ProductId == id && x.UserName == userName);

            if (cartItem != null && cartItem.Quantity > 1)
{
    cartItem.Quantity--;
    _context.SaveChanges();

    UpdateCartCount();
}

            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var userName = HttpContext.Session.GetString("UserName");

            var cartItem = _context.CartItems
                .FirstOrDefault(x => x.ProductId == id && x.UserName == userName);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();

                UpdateCartCount();
            }

            return RedirectToAction("Index");
        }

        public IActionResult CompleteOrder()
        {
            var userName = HttpContext.Session.GetString("UserName");

            var cartItems = _context.CartItems
                .Where(x => x.UserName == userName)
                .ToList();

            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();

            TempData["OrderMessage"] = "Sipariş başarıyla oluşturuldu!";
            return RedirectToAction("Index");
        }
    }
}




