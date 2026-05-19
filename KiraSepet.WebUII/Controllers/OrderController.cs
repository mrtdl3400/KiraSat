using KiraSepet.WebUI.Models;
using KiraSepet.WebUI.Controllers;
using Microsoft.AspNetCore.Mvc;
using KiraSepet.EntityLayer;
using Microsoft.EntityFrameworkCore;
using KiraSepet.DataAccessLayer;
namespace KiraSepet.WebUI.Controllers
{
    public class OrderController : Controller
    {
        private readonly Context _context;

        public OrderController(Context context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var values = _context.Orders
                .Where(x => x.UserEmail == userEmail)
                .ToList();

            return View(values);
        }
    }
}
