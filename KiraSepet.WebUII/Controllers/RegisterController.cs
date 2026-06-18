
using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity;

namespace KiraSepet.WebUI.Controllers
{
    public class RegisterController : Controller
    {
        private readonly Context _context;

        public RegisterController(Context context)
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
            appUser.Role = "User";

            var passwordHasher = new PasswordHasher<AppUser>();
            appUser.Password = passwordHasher.HashPassword(appUser, appUser.Password);

            _context.AppUsers.Add(appUser);
            _context.SaveChanges();

            return RedirectToAction("Index", "Home"); //Kullanıcının kayıt olacağı backend sistemini oluşturdum.

            //AppUser classı oluşturduk
            //Context içine DbSet<AppUser> ekledik
             //RegisterController yaptık
             //GET ve POST action mantığını kullandık
             //Formdan gelen veriyi _context.AppUsers.Add() ile veritabanına kaydettik
            //SaveChanges() ile SQL’e işledik
            //RedirectToAction() ile başka sayfaya yönlendirdik
        }
    }
}
