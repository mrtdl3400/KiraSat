using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KiraSepet.WebUI.Controllers
{
    public class SellerController : Controller
    {
        private readonly Context _context;

        public SellerController(Context context)
        {
            _context = context;
        }

        private Business? GetCurrentBusiness()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(userEmail))
                return null;

            var user = _context.AppUsers
                .FirstOrDefault(x => x.Email == userEmail);

            if (user == null)
                return null;

            return _context.Businesses
                .FirstOrDefault(x => x.OwnerUserId == user.Id && x.IsApproved);
        }

        public IActionResult Dashboard()
        {
            var business = GetCurrentBusiness();

            if (business == null)
                return RedirectToAction("Apply", "Business");

            var products = _context.Products
                .Include(x => x.Category)
                .Where(x => x.BusinessId == business.Id && !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .ToList();

            ViewBag.Categories = _context.Categories
                .OrderBy(x => x.CategoryName)
                .ToList();

            ViewBag.BusinessName = business.CompanyName;

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(Product product)
        {
            var business = GetCurrentBusiness();

            if (business == null)
                return RedirectToAction("Apply", "Business");

            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                TempData["Error"] = "Ürün adı boş bırakılamaz.";
                return RedirectToAction(nameof(Dashboard));
            }

            if (!_context.Categories.Any(x => x.Id == product.CategoryId))
            {
                TempData["Error"] = "Lütfen geçerli bir kategori seç.";
                return RedirectToAction(nameof(Dashboard));
            }

            product.Id = 0;
            product.BusinessId = business.Id;
            product.IsDeleted = false;

            if (!product.IsRentable)
            {
                product.DailyPrice = null;
                product.RentType = null;
            }

            _context.Products.Add(product);
            _context.SaveChanges();

            TempData["Success"] = "Ürün başarıyla eklendi.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public IActionResult EditProduct(int id)
        {
            var business = GetCurrentBusiness();

            if (business == null)
                return RedirectToAction("Apply", "Business");

            var product = _context.Products
                .FirstOrDefault(x => x.Id == id &&
                                     x.BusinessId == business.Id &&
                                     !x.IsDeleted);

            if (product == null)
                return NotFound();

            ViewBag.Categories = _context.Categories
                .OrderBy(x => x.CategoryName)
                .ToList();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(Product product)
        {
            var business = GetCurrentBusiness();

            if (business == null)
                return RedirectToAction("Apply", "Business");

            var existingProduct = _context.Products
                .FirstOrDefault(x => x.Id == product.Id &&
                                     x.BusinessId == business.Id &&
                                     !x.IsDeleted);

            if (existingProduct == null)
                return NotFound();

            existingProduct.ProductName = product.ProductName;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.SalePrice = product.SalePrice;
            existingProduct.StockCount = product.StockCount;
            existingProduct.IsRentable = product.IsRentable;
            existingProduct.DailyPrice = product.IsRentable ? product.DailyPrice : null;
            existingProduct.RentType = product.IsRentable ? product.RentType : null;

            _context.SaveChanges();

            TempData["Success"] = "Ürün başarıyla güncellendi.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int id)
        {
            var business = GetCurrentBusiness();

            if (business == null)
                return RedirectToAction("Apply", "Business");

            var product = _context.Products
                .FirstOrDefault(x => x.Id == id && x.BusinessId == business.Id && !x.IsDeleted);

            if (product == null)
                return NotFound();

            product.IsDeleted = true;
            _context.SaveChanges();

            TempData["Success"] = "Ürün yayından kaldırıldı.";
            return RedirectToAction(nameof(Dashboard));
        }

    }
}
