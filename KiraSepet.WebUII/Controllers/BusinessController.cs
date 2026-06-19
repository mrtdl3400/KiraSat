using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using KiraSepet.WebUII.Models;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUII.Controllers
{
    public class BusinessController : Controller
    {
        private readonly Context _context;

        public BusinessController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Apply()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var user = _context.AppUsers
                .FirstOrDefault(x => x.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Logout", "Login");
            }

            var existingBusiness = _context.Businesses
                .FirstOrDefault(x => x.OwnerUserId == user.Id);

            if (existingBusiness != null)
            {
                TempData["BusinessMessage"] = existingBusiness.Status switch
                {
                    BusinessStatus.Pending =>
                        "İşletme başvurunuz admin onayı bekliyor.",

                    BusinessStatus.Approved =>
                        "İşletmeniz aktif. Satıcı işlemlerine devam edebilirsiniz.",

                    BusinessStatus.Suspended =>
                        "İşletmeniz admin tarafından askıya alındı.",

                    BusinessStatus.Rejected =>
                        "İşletme başvurunuz reddedildi.",

                    _ => "İşletme durumunuz görüntülenemedi."
                };

                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Apply(BusinessApplicationViewModel model)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var user = _context.AppUsers
                .FirstOrDefault(x => x.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Logout", "Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var taxNumber = model.TaxNumber.Trim();

            var taxNumberExists = _context.Businesses
                .Any(x => x.TaxNumber == taxNumber);

            if (taxNumberExists)
            {
                ModelState.AddModelError("TaxNumber",
                    "Bu vergi numarasıyla kayıtlı bir işletme zaten var.");

                return View(model);
            }

            var existingBusiness = _context.Businesses
                .Any(x => x.OwnerUserId == user.Id);

            if (existingBusiness)
            {
                TempData["BusinessMessage"] =
                    "Zaten bir işletme başvurunuz bulunuyor.";

                return RedirectToAction("Index", "Home");
            }

            var business = new Business
            {
                CompanyName = model.CompanyName.Trim(),
                TaxNumber = taxNumber,
                OwnerUserId = user.Id,
                IsApproved = false,
                Status = BusinessStatus.Pending,
                CommissionRate = 10m
            };

            _context.Businesses.Add(business);
            _context.SaveChanges();

            TempData["BusinessMessage"] =
                "İşletme başvurunuz alındı. Admin onayı bekleniyor.";

            return RedirectToAction("Index", "Home");
        }
    }
}
