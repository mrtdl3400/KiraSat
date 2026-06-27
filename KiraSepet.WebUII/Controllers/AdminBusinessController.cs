using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KiraSepet.WebUII.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBusinessController : Controller
    {
        private readonly Context _context;

        public AdminBusinessController(Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var businesses = _context.Businesses
                .OrderBy(x => x.Status)
                .ThenByDescending(x => x.Id)
                .ToList();

            var ownerIds = businesses.Select(x => x.OwnerUserId).Distinct().ToList();

            ViewBag.OwnerNames = _context.AppUsers
                .Where(x => ownerIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.NameSurname);

            ViewBag.OwnerEmails = _context.AppUsers
                .Where(x => ownerIds.Contains(x.Id))
                .ToDictionary(x => x.Id, x => x.Email);

            return View(businesses);
        }

        public IActionResult Products(int id)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var business = _context.Businesses.Find(id);
            if (business == null)
            {
                return NotFound();
            }

            ViewBag.Business = business;

            var products = _context.Products
                .Where(x => x.BusinessId == id && !x.IsDeleted)
                .OrderByDescending(x => x.Id)
                .ToList();

            return View(products);
        }

        [HttpGet]
        public IActionResult PaymentTerms()
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var terms = _context.LegalTexts.FirstOrDefault(x => x.Key == "PaymentTerms");

            if (terms == null)
            {
                terms = new LegalText
                {
                    Key = "PaymentTerms",
                    Title = "Kiralama ve satın alma bilgilendirme metni",
                    Content = GetDefaultPaymentTerms(),
                    UpdatedAt = DateTime.UtcNow
                };
            }

            return View(terms);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaymentTerms(LegalText model)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            if (string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Content))
            {
                TempData["BusinessError"] = "Başlık ve metin alanı boş bırakılamaz.";
                model.Key = "PaymentTerms";
                return View(model);
            }

            var terms = _context.LegalTexts.FirstOrDefault(x => x.Key == "PaymentTerms");

            if (terms == null)
            {
                terms = new LegalText
                {
                    Key = "PaymentTerms"
                };

                _context.LegalTexts.Add(terms);
            }

            terms.Title = model.Title.Trim();
            terms.Content = model.Content.Trim();
            terms.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            TempData["BusinessMessage"] = "Ödeme ve kiralama şartları güncellendi.";
            return RedirectToAction(nameof(PaymentTerms));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var business = _context.Businesses.Find(id);
            var owner = business == null
                ? null
                : _context.AppUsers.Find(business.OwnerUserId);

            if (business == null || owner == null)
            {
                TempData["BusinessError"] = "İşletme veya sahibi bulunamadı.";
                return RedirectToAction("Index");
            }

            business.Status = BusinessStatus.Approved;
            business.IsApproved = true;
            owner.Role = "Seller";
            CreateNotification(
            owner.Id,
            "İşletmeniz onaylandı",
            $"{business.CompanyName} işletmeniz onaylandı. Satıcı işlemlerine başlayabilirsiniz.");

            _context.SaveChanges();

            TempData["BusinessMessage"] = "İşletme onaylandı.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Suspend(int id)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var business = _context.Businesses.Find(id);
            var owner = business == null
                ? null
                : _context.AppUsers.Find(business.OwnerUserId);

            if (business == null || owner == null)
            {
                TempData["BusinessError"] = "İşletme veya sahibi bulunamadı.";
                return RedirectToAction("Index");
            }

            business.Status = BusinessStatus.Suspended;
            business.IsApproved = false;
            owner.Role = "User";

            CreateNotification(
            owner.Id,
            "İşletmeniz askıya alındı",
            $"{business.CompanyName} işletmeniz admin tarafından askıya alındı.");

            _context.SaveChanges();

            TempData["BusinessMessage"] =
                "İşletme askıya alındı. Satıcının yetkisi kaldırıldı.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reactivate(int id)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var business = _context.Businesses.Find(id);
            var owner = business == null
                ? null
                : _context.AppUsers.Find(business.OwnerUserId);

            if (business == null || owner == null)
            {
                TempData["BusinessError"] = "İşletme veya sahibi bulunamadı.";
                return RedirectToAction("Index");
            }

            business.Status = BusinessStatus.Approved;
            business.IsApproved = true;
            owner.Role = "Seller";

            CreateNotification(
            owner.Id,
            "İşletmeniz tekrar aktif edildi",
            $"{business.CompanyName} işletmeniz yeniden aktif edildi.");

            _context.SaveChanges();

            TempData["BusinessMessage"] =
                "İşletme tekrar aktif edildi.";

            return RedirectToAction("Index");
        }

        [HttpPost]

        [ValidateAntiForgeryToken]
        public IActionResult RemoveProduct(int id)
        {
            var adminCheck = RedirectIfNotAdmin();
            if (adminCheck != null)
            {
                return adminCheck;
            }

            var product = _context.Products.FirstOrDefault(x => x.Id == id && !x.IsDeleted);

            if (product == null || product.BusinessId == null)
            {
                TempData["BusinessError"] = "Ürün bulunamadı.";
                return RedirectToAction("Index");
            }

            var business = _context.Businesses.Find(product.BusinessId.Value);

            product.IsDeleted = true;

            if (business != null)
            {
                CreateNotification(
                    business.OwnerUserId,
                    "Ürününüz yayından kaldırıldı",
                    $"{product.ProductName} ilanınız admin tarafından yayından kaldırıldı.");
            }

            _context.SaveChanges();

            TempData["BusinessMessage"] = "Ürün yayından kaldırıldı.";
            return RedirectToAction(nameof(Products), new { id = product.BusinessId.Value });
        }
        private void CreateNotification(int userId, string title, string message)
        {
            _context.AppNotifications.Add(new AppNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        private string GetDefaultPaymentTerms()
        {
            return @"KiraSat üzerinden yapılan satın alma ve kiralama işlemlerinde kullanıcı, seçtiği ürünü açıklamada belirtilen niteliklere göre incelediğini ve işlem bedelini onayladığını kabul eder.

Kiralama işlemlerinde kullanıcı, ürünü teslim aldığı haliyle korumakla, kullanım süresi boyunca ürüne makul özeni göstermekle ve ürünü kararlaştırılan tarihte eksiksiz şekilde iade etmekle sorumludur.

Ürünün teslim alınması sırasında kullanıcı, üründe görünür bir hasar veya eksiklik olup olmadığını kontrol etmelidir. Teslim sonrası ortaya çıkan hasar, kayıp, eksik parça veya geç iade durumlarında satıcı ile kullanıcı arasında sorumluluk değerlendirmesi yapılabilir.

Satıcı, ilana eklediği ürün bilgilerinin doğru olduğunu, ürünün kullanıma uygun durumda bulunduğunu ve teslim sürecinde kullanıcıyı yanıltıcı bilgi vermeyeceğini kabul eder.

Platform, kullanıcı ile satıcı arasındaki alışveriş ve kiralama sürecini kolaylaştıran aracı bir hizmet sunar. Taraflar, işlemle ilgili mesajlaşma ve bildirimleri takip etmekle sorumludur.";
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        private IActionResult? RedirectIfNotAdmin()
        {
            if (!IsAdmin())
            {
                TempData["AuthError"] =
                    "Bu işlem için admin yetkisi gerekiyor.";

                return RedirectToAction("Index", "Product");
            }

            return null;
        }
    }
}
