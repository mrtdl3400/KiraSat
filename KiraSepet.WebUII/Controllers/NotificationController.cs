using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUII.Controllers
{
    public class NotificationController : Controller
    {
        private readonly Context _context;

        public NotificationController(Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var user = GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var notifications = _context.AppNotifications
                .Where(x => x.UserId == user.Id &&
                    x.Title != "Yeni iletişim mesajı" &&
                    x.Title != "Kullanıcı yanıtı" &&
                    x.Title != "Destek yanıtı")
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var unreadNotifications = notifications
                .Where(x => !x.IsRead)
                .ToList();

            if (unreadNotifications.Count > 0)
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                _context.SaveChanges();
            }

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAsRead()
        {
            var user = GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var unreadNotifications = _context.AppNotifications
                .Where(x => x.UserId == user.Id && !x.IsRead &&
                    x.Title != "Yeni iletişim mesajı" &&
                    x.Title != "Kullanıcı yanıtı" &&
                    x.Title != "Destek yanıtı")
                .ToList();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult OpenBusinessRequest(int id)
        {
            var user = GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction("Index", "Login");
            }

            var notification = _context.AppNotifications
                .FirstOrDefault(x => x.Id == id && x.UserId == user.Id);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            return user.Role == "Admin"
                ? RedirectToAction("Index", "AdminBusiness")
                : RedirectToAction("Index");
        }

        public IActionResult OpenContactMessage(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToAction("Index", "Login");

            var notification = _context.AppNotifications.FirstOrDefault(x => x.Id == id && x.UserId == user.Id);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }

            return user.Role == "Admin"
                ? RedirectToAction("ContactMessages", "Account")
                : RedirectToAction("MyMessages", "Account");
        }

        private AppUser? GetCurrentUser()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return null;
            }

            return _context.AppUsers
                .FirstOrDefault(x => x.Email == userEmail);
        }
    }
}
