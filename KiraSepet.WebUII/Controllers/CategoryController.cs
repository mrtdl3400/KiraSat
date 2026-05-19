
using KiraSepet.DataAccessLayer;
using KiraSepet.EntityLayer;
using Microsoft.AspNetCore.Mvc;


namespace KiraSepet.WebUI.Controllers
{
    public class CategoryController : Controller
    {
        private readonly Context _context;

        public CategoryController(Context context)
        {
            _context = context;
        }

        public IActionResult AddCategory()
        {
            return View();
        }

        
        public IActionResult Delete(int id)
        {
            var value = _context.Categories.Find(id);

            if (value == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(value);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }
        public IActionResult index()
        {
            var values = _context.Categories.ToList();
            return View(values);
        }   
    } 
    
}
