using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUII.Controllers
{
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

            return View(businesses);
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