using KiraSepet.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;

namespace KiraSepet.WebUII.Controllers;

public class SellerProfileController : Controller
{
    private readonly Context _context;

    public SellerProfileController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index(int businessId)
    {
        var business = _context.Businesses
            .FirstOrDefault(x => x.Id == businessId && x.IsApproved);

        if (business == null)
        {
            return NotFound();
        }

        var owner = _context.AppUsers
            .FirstOrDefault(x => x.Id == business.OwnerUserId);

        var currentUserEmail = HttpContext.Session.GetString("UserEmail");
        var currentUser = string.IsNullOrWhiteSpace(currentUserEmail)
            ? null
            : _context.AppUsers.FirstOrDefault(x => x.Email == currentUserEmail);

        var products = _context.Products
            .Where(x => x.BusinessId == businessId && !x.IsDeleted)
            .OrderByDescending(x => x.Id)
            .ToList();

        ViewBag.Business = business;
        ViewBag.OwnerName = owner?.NameSurname ?? "Satıcı";
        ViewBag.ProductCount = products.Count;
        ViewBag.IsOwner = currentUser?.Id == business.OwnerUserId;

        return View(products);
    }
}
