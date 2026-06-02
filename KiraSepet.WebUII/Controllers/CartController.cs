using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KiraSepet.WebUI.Controllers
{
    
    

    public class CartController : Controller
    {
        private readonly Context _context;

        public CartController(Context context)
        {
            _context = context;
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

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var cartItems = GetCart();
            return View(cartItems);
        }

        public IActionResult AddToCart(int id)
        {
            if (HttpContext.Session.GetString("UserName") == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var product = _context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }

            var cartItems = GetCart();

            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem == null)
            {
                cartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    SalePrice = product.SalePrice,
                    Quantity = 1
                });
            }
            else
            {
                cartItem.Quantity++;
            }

            SaveCart(cartItems);

            TempData["Message"] = "Ürün sepete eklendi!";

            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int id)
        {
            var cartItems = GetCart();

            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null)
            {
                cartItem.Quantity++;
            }

            SaveCart(cartItems);

            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int id)
        {
            var cartItems = GetCart();

            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null && cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
            }

            SaveCart(cartItems);

            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cartItems = GetCart();

            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null)
            {
                cartItems.Remove(cartItem);
            }

            SaveCart(cartItems);

            return RedirectToAction("Index");
        }

        public IActionResult CompleteOrder()
        {
            var cartItems = new List<CartItem>();
            SaveCart(cartItems);

            TempData["OrderMessage"] = "Sipariş başarıyla oluşturuldu!";

            return RedirectToAction("Index");
        }
    }
}
