using Microsoft.AspNetCore.Mvc;
using KiraSepet.WebUI.Models;
using KiraSepet.DataAccessLayer;
namespace KiraSepet.WebUII.Controllers
{
    public class CartController : Controller
    {
        private readonly Context _context;

        public CartController(Context context)
        {
            _context = context;
        }

        public static List<CartItem> cartItems = new List<CartItem>();
        public IActionResult Index()
        {
            return View(cartItems);
        }
        public IActionResult AddToCart(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {

                return RedirectToAction("Index", "Product");
            }

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
            TempData["Message"] = "Ürün sepete eklendi!";

            return RedirectToAction("Index");
        }
        public IActionResult IncreaseQuantity(int id)
        {
            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null)
            {
                cartItem.Quantity++;
            }

            return RedirectToAction("Index");
        }
        public IActionResult DecreaseQuantity(int id)
        {
            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null && cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
            }

            return RedirectToAction("Index");
        }
        public IActionResult RemoveFromCart(int id)
        {
            var cartItem = cartItems.FirstOrDefault(x => x.ProductId == id);

            if (cartItem != null)
            {
                cartItems.Remove(cartItem);
            }

            return RedirectToAction("Index");
        }
        public IActionResult CompleteOrder()
        {
            cartItems.Clear();

            TempData["OrderMessage"] = "Sipariş başarıyla oluşturuldu!";

            return RedirectToAction("Index");
        }
    }
}
