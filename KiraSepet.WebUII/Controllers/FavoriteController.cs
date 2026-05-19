using Microsoft.AspNetCore.Mvc;
using KiraSepet.WebUII.Models;
using KiraSepet.DataAccessLayer;

namespace KiraSepet.WebUII.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly Context _context;

        public FavoriteController(Context context)
        {
            _context = context;
        }

        public static List<FavoriteItem> favoriteItems = new List<FavoriteItem>();

        public IActionResult Index()
        {
            return View(favoriteItems);
        }
        public IActionResult AddToFavorite(int id)
        {
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Product");
            }

            var favoriteItem = favoriteItems.FirstOrDefault(x => x.ProductId == id);

            if (favoriteItem == null)
            {
                favoriteItems.Add(new FavoriteItem
                {
                    ProductId = product.Id,
                    ProductName = product.ProductName,
                    SalePrice = product.SalePrice
                });
            }

            return RedirectToAction("Index");
        }
    }
}
