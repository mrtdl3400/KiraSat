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

            var rentalValues = _context.RentalOrders
                .Where(x => x.UserEmail == userEmail)
                .ToList();

            ViewBag.RentalOrders = rentalValues;

            return View(values);
        }

        public IActionResult PurchasedOrders()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail")
                ?? HttpContext.Session.GetString("UserName");

            var values = _context.Orders
                .Where(x => x.UserEmail == userEmail)
                .ToList();

            return View("Index", values);
        }

        public IActionResult RentedOrders()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            var rentalValues = _context.RentalOrders
                .Where(x => x.UserEmail == userEmail)
                .ToList();

            ViewBag.RentalOrders = rentalValues;

            return View("Index", new List<KiraSepet.EntityLayer.Order>());
        }


    }
}
