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
    .FirstOrDefault(x => x.Email == appUser.Email
    && x.Password == appUser.Password);

            Console.WriteLine(appUser.Email);
            Console.WriteLine(appUser.Password);


            if (user != null)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.NameSurname);
                HttpContext.Session.SetString("UserRole", user.Role);

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
