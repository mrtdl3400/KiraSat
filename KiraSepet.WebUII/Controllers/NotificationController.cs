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
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

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
                .Where(x => x.UserId == user.Id && !x.IsRead)
                .ToList();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
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
