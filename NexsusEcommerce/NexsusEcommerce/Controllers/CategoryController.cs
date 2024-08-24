using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;

namespace NexsusEcommerce.Controllers
{
    public class CategoryController : Controller
    {
       
        private readonly EcommerceContext _context;

        public CategoryController(EcommerceContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList(); // Assuming _context is your database context
            return View(categories);
        }
    }
}
