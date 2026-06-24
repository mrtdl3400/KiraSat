using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;


namespace KiraSepet.WebUII.Controllers;




public class ProductController : Controller
{
    private readonly Context _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductController(Context context, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text.ToLower()
            .Replace("ı", "i")
            .Replace("ğ", "g")
            .Replace("ü", "u")
            .Replace("ş", "s")
            .Replace("ö", "o")
            .Replace("ç", "c");
    }
    public IActionResult Index(string p, string category, string city, string district)
    {
        var values = _context.Products
            .Include(x => x.Category)
            .Where(x => !x.IsDeleted)
            .ToList();


        if (!string.IsNullOrEmpty(category))
        {
            values = values.Where(x => x.Category.CategoryName == category).ToList();
        }

        if (!string.IsNullOrEmpty(p))
        {
            p = NormalizeText(p);

            values = values.Where(x =>
                x.ProductName != null &&
                NormalizeText(x.ProductName).Contains(p)
            ).ToList();
        }

        city = NormalizeText(city);
        district = NormalizeText(district);

        if (!string.IsNullOrEmpty(city))
        {
            values = values.Where(x =>
            x.City != null &&
           NormalizeText(x.City).Contains(city)).ToList();
        }

        if (!string.IsNullOrEmpty(district))
        {
            values = values.Where(x =>
    x.District != null &&
    NormalizeText(x.District).Contains(district)).ToList();
        }

        ViewBag.ProductAverageRatings = _context.Comments
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Average(x => x.Rating)
            );

        ViewBag.ProductRatingCounts = _context.Comments
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Count()
            );

        return View(values.ToList());
    }

    public IActionResult AddProduct()
    {
        var adminCheck = RedirectIfNotAdmin();
        if (adminCheck != null)
        {
            return adminCheck;
        }

        ViewBag.Categories = _context.Categories.ToList();

        return View();
    }
    [HttpPost]
    public IActionResult AddProduct(Product p, IFormFile imageFile)
    {
        var adminCheck = RedirectIfNotAdmin();
        if (adminCheck != null)
        {
            return adminCheck;
        }

        if (imageFile != null)
        {
            var extension = Path.GetExtension(imageFile.FileName);
            var newImageName = Guid.NewGuid() + extension;

            var location = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/images/products/", newImageName);

            var stream = new FileStream(location, FileMode.Create);
            imageFile.CopyTo(stream);

            p.ImageUrl = "/images/products/" + newImageName;
        }

        if (string.IsNullOrWhiteSpace(p.Description))
        {
            p.Description = "";
        }

        _context.Products.Add(p);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateProduct(Product p, IFormFile imageFile)
    {
        var adminCheck = RedirectIfNotAdmin();
        if (adminCheck != null)
        {
            return adminCheck;
        }

        var values = _context.Products.Find(p.Id);

        if (values == null)
        {
            return NotFound();
        }

        values.ProductName = p.ProductName;
        values.Brand = string.IsNullOrWhiteSpace(p.Brand) ? values.Brand : p.Brand;
        values.SalePrice = p.SalePrice;
        values.DailyPrice = p.DailyPrice;
        values.CategoryId = p.CategoryId;
        values.IsRentable = p.IsRentable;
        values.StockCount = p.StockCount;
        
        values.Description = p.Description ?? "";
        values.City = p.City ?? "";
        values.District = p.District ?? "";
        values.Address = p.Address ?? "";

        if (imageFile != null)
        {
            var extension = Path.GetExtension(imageFile.FileName);
            var newImageName = Guid.NewGuid() + extension;
            var location = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/images/products/", newImageName);

            var stream = new FileStream(location, FileMode.Create);
            imageFile.CopyTo(stream);

            values.ImageUrl = "/images/products/" + newImageName;
        }

        _context.SaveChanges();

        return RedirectToAction("Index");
    }
    public IActionResult UpdateProduct(int id)
    {
        var adminCheck = RedirectIfNotAdmin();
        if (adminCheck != null)
        {
            return adminCheck;
        }

        ViewBag.Categories = _context.Categories.ToList();


        var value = _context.Products.Find(id);
        return View(value);


    }


    [HttpPost]
    public IActionResult DeleteProduct(int id)
    {
        var adminCheck = RedirectIfNotAdmin();
        if (adminCheck != null)
        {
            return adminCheck;
        }

        var value = _context.Products.Find(id);

        if (value == null)
        {
            return NotFound();
        }

        _context.Products.Remove(value);
        _context.SaveChanges();

        return RedirectToAction("Index");
    }
    public IActionResult Add()
    {
        ViewBag.Categories = _context.Categories.ToList();
        return View();
    }
    public IActionResult ProductDetails(int id)
    {
        var values = _context.Products
    .Include(x => x.Business)
    .FirstOrDefault(x => x.Id == id && !x.IsDeleted);

        if (values == null)
        {
            return NotFound();
        }

        ViewBag.comments = _context.Comments
            .Where(x => x.ProductId == id)
            .OrderByDescending(x => x.CreatedDate)
            .ToList();

        var ratings = _context.Comments
    .Where(x => x.ProductId == id)
    .Select(x => x.Rating)
    .ToList();

        ViewBag.AverageRating = ratings.Any() ? ratings.Average() : 0;
        ViewBag.RatingCount = ratings.Count;

        return View(values);
    }
    private bool IsAdmin()
    {
        return HttpContext.Session.GetString("UserRole") == "Admin";
    }

    private IActionResult? RedirectIfNotAdmin()
    {
        if (!IsAdmin())
        {
            TempData["AuthError"] = "Bu işlem için admin yetkisi gerekiyor.";
            return RedirectToAction("Index", "Product");
        }

        return null;
    }
   

    [HttpPost]
    
    public IActionResult AddComment(Comment comment)
    {
        comment.UserName = HttpContext.Session.GetString("UserName") ?? "Misafir";

        bool puanVarMi = _context.Comments
            .Any(x => x.ProductId == comment.ProductId
                   && x.UserName == comment.UserName);

        if (puanVarMi)
        {
            TempData["RatingError"] = "Üzgünüz, bu ürüne daha önce puan verdiniz.";

            return RedirectToAction("ProductDetails", new { id = comment.ProductId });
        }

        comment.CommentText = "Puanlama";
        comment.CreatedDate = DateTime.Now;

        _context.Comments.Add(comment);
        _context.SaveChanges();

        return RedirectToAction("ProductDetails", new { id = comment.ProductId });
    }
}





