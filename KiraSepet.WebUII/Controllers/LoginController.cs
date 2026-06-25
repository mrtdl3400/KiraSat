using Microsoft.AspNetCore.Identity;
using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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
        public async Task<IActionResult> Index(AppUser appUser)
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
                if (!user.IsEmailVerified)
                {
                    ViewBag.Error = "Giriş yapmadan önce e-posta adresinizi doğrulamanız gerekiyor.";
                    return View();
                }

                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.NameSurname);
                HttpContext.Session.SetString("UserRole", user.Role);

                var cartCount = _context.CartItems
                .Where(x => x.UserName == user.NameSurname)
                .Sum(x => x.Quantity);

                HttpContext.Session.SetInt32("CartCount", cartCount);


                var claims = new List<Claim>
                {
    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new(ClaimTypes.Name, user.NameSurname),
    new(ClaimTypes.Email, user.Email),
    new(ClaimTypes.Role, user.Role)
};

                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = true
                    });
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Product");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Error = "E-posta adresiniz veya şifreniz hatalı.";
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Login");
        }
    }
}
