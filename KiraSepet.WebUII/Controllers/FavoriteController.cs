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

        public static Dictionary<string, List<FavoriteItem>> userFavorites = new Dictionary<string, List<FavoriteItem>>();

        public IActionResult Index()
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Guest";

            if (!userFavorites.ContainsKey(userName))
            {
                userFavorites[userName] = new List<FavoriteItem>();
            }

            return View(userFavorites[userName]);
        }
        public IActionResult AddToFavorite(int id)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Guest";

            if (!userFavorites.ContainsKey(userName))
            {
                userFavorites[userName] = new List<FavoriteItem>();
            }

            var favoriteItems = userFavorites[userName];
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

        public IActionResult RemoveFromFavorite(int id)
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "Guest";

            if (!userFavorites.ContainsKey(userName))
            {
                userFavorites[userName] = new List<FavoriteItem>();
            }

            var favoriteItems = userFavorites[userName];
            var favoriteItem = favoriteItems.FirstOrDefault(x => x.ProductId == id);

            if (favoriteItem != null)
            {
                favoriteItems.Remove(favoriteItem);
            }

            return RedirectToAction("Index");
        }
    }
}
