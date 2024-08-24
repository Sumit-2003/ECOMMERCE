using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;
using System.Linq;

namespace NexsusEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly EcommerceContext _context;

        public HomeController(EcommerceContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Fetch the list of categories from the database
            var categories = _context.Categories.ToList();
            return View(categories);
        }
    }
}
