using Microsoft.AspNetCore.Identity;
using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace KiraSepet.WebUI.Controllers
{
    public class LoginController : Controller
    {
        private readonly Context _context;
        
        public LoginController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(AppUser appUser)
        {
            var user = _context.AppUsers
    .FirstOrDefault(x => x.Email == appUser.Email);

            var passwordHasher = new PasswordHasher<AppUser>();
            var passwordResult = PasswordVerificationResult.Failed;

            if (user != null)
            {
                try
                {
                    passwordResult = passwordHasher.VerifyHashedPassword(user, user.Password, appUser.Password);
                }
                catch (FormatException)
                {
                    passwordResult = PasswordVerificationResult.Failed;
                }

                if (passwordResult == PasswordVerificationResult.Failed && user.Password == appUser.Password)
                {
                    user.Password = passwordHasher.HashPassword(user, appUser.Password);
                    _context.SaveChanges();

                    passwordResult = PasswordVerificationResult.Success;
                }
            }


            if (user != null && passwordResult != PasswordVerificationResult.Failed)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.NameSurname);
                HttpContext.Session.SetString("UserRole", user.Role);

                var cartCount = _context.CartItems
                .Where(x => x.UserName == user.NameSurname)
                .Sum(x => x.Quantity);

                HttpContext.Session.SetInt32("CartCount", cartCount);

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "Email veya şifre hatalı";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
