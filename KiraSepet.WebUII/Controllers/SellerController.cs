using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace KiraSepet.WebUI.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;


        public SellerController(Context context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        private const long MaxImageFileSize = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private bool TrySaveProductImage(IFormFile imageFile, out string imageUrl, out string errorMessage)
        {
            imageUrl = string.Empty;
            errorMessage = string.Empty;

            var extension = Path.GetExtension(imageFile.FileName);
            if (imageFile.Length == 0)
            {
                errorMessage = "Yüklenen görsel boş olamaz.";
                return false;
            }

            if (imageFile.Length > MaxImageFileSize)
            {
                errorMessage = "Görsel dosyası en fazla 5 MB olabilir.";
                return false;
            }

            if (!AllowedImageExtensions.Contains(extension) || !HasValidImageSignature(imageFile, extension))
            {
                errorMessage = "Sadece JPG, JPEG, PNG veya WEBP formatında geçerli görseller yükleyebilirsin.";
                return false;
            }

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var directory = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            Directory.CreateDirectory(directory);

            using var output = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew);
            imageFile.CopyTo(output);
            imageUrl = $"/images/products/{fileName}";
            return true;
        }

        private static bool HasValidImageSignature(IFormFile imageFile, string extension)
        {
            var header = new byte[12];
            using var input = imageFile.OpenReadStream();
            var bytesRead = input.Read(header, 0, header.Length);

            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
                ".png" => bytesRead >= 8 && header.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                ".webp" => bytesRead >= 12 && header.Take(4).SequenceEqual("RIFF"u8.ToArray()) && header.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray()),
                _ => false
            };
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
        public IActionResult AddProduct(Product product, IFormFile imageFile)
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
                product.RentalStockCount = 0;
            }

            if (imageFile != null)
            {
                if (!TrySaveProductImage(imageFile, out var imageUrl, out var imageError))
                {
                    TempData["Error"] = imageError;
                    return RedirectToAction(nameof(Dashboard));
                }

                product.ImageUrl = imageUrl;
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
        public IActionResult EditProduct(Product product, IFormFile imageFile)
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
            existingProduct.RentalStockCount = product.IsRentable ? product.RentalStockCount : 0;

            if (imageFile != null)
            {
                if (!TrySaveProductImage(imageFile, out var imageUrl, out var imageError))
                {
                    TempData["Error"] = imageError;
                    return RedirectToAction(nameof(EditProduct), new { id = product.Id });
                }

                existingProduct.ImageUrl = imageUrl;
            }

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

    

    public IActionResult RentalRequests()
        {
            var business = GetCurrentBusiness();

            if (business == null)
            {
                return RedirectToAction("Apply", "Business");
            }

            var productIds = _context.Products
                .Where(x => x.BusinessId == business.Id && !x.IsDeleted)
                .Select(x => x.Id)
                .ToList();

            var requests = _context.RentalOrders
                .Where(x => productIds.Contains(x.ProductId) &&
                            x.Status == "Satıcı Onayı Bekleniyor")
                .OrderByDescending(x => x.OrderDate)
                .ToList();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveRental(int id)
        {
            var business = GetCurrentBusiness();

            if (business == null)
            {
                return RedirectToAction("Apply", "Business");
            }

            var rentalOrder = _context.RentalOrders.FirstOrDefault(x => x.Id == id);
            var product = rentalOrder == null
                ? null
                : _context.Products.FirstOrDefault(x =>
                    x.Id == rentalOrder.ProductId &&
                    x.BusinessId == business.Id &&
                    !x.IsDeleted);

            if (rentalOrder == null || product == null ||
                rentalOrder.Status != "Satıcı Onayı Bekleniyor")
            {
                return NotFound();
            }

            if (product.RentalStockCount < rentalOrder.Quantity)
            {
                TempData["Error"] = "Ürün için yeterli kiralama stoğu bulunmuyor.";
                return RedirectToAction(nameof(RentalRequests));
            }

            rentalOrder.Status = "Onaylandı";
            product.RentalStockCount -= rentalOrder.Quantity;

            var tenant = _context.AppUsers
                .FirstOrDefault(x => x.Email == rentalOrder.UserEmail);

            if (tenant != null)
            {
                _context.AppNotifications.Add(new AppNotification
                {
                    UserId = tenant.Id,
                    Title = "Kiralama talebiniz onaylandı",
                    Message = $"{rentalOrder.ProductName} için {rentalOrder.StartDate:dd.MM.yyyy} - {rentalOrder.EndDate:dd.MM.yyyy} tarihleri arasındaki kiralama talebiniz onaylandı.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.SaveChanges();

            TempData["Success"] = "Kiralama talebi onaylandı.";
            return RedirectToAction(nameof(RentalRequests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectRental(int id)
        {
            var business = GetCurrentBusiness();

            if (business == null)
            {
                return RedirectToAction("Apply", "Business");
            }

            var rentalOrder = _context.RentalOrders
                .FirstOrDefault(x => x.Id == id);

            var productBelongsToBusiness = rentalOrder != null &&
                _context.Products.Any(x =>
                    x.Id == rentalOrder.ProductId &&
                    x.BusinessId == business.Id &&
                    !x.IsDeleted);

            if (rentalOrder == null || !productBelongsToBusiness ||
                rentalOrder.Status != "Satıcı Onayı Bekleniyor")
            {
                return NotFound();
            }

            rentalOrder.Status = "Reddedildi";
            _context.SaveChanges();

            TempData["Success"] = "Kiralama talebi reddedildi.";
            return RedirectToAction(nameof(RentalRequests));
        }
    } }
