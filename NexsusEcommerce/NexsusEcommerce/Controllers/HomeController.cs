using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NexsusEcommerce.Controllers
{
    public class HomeController : Controller
    {
        private readonly EcommerceContext _context;

        public HomeController(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CreateHome()
        {
            // Fetch products and associated images from the database
            var products = await _context.Products
                .Include(p => p.ProductImages) // Ensure images are included
                .ToListAsync();

            // Optionally, fetch categories if needed for other parts of the view
            var categories = await _context.Categories.ToListAsync();

            // Create a view model to pass to the view
            var viewModel = new HomeViewModel
            {
                Products = products,
                Categories = categories
            };

            return View(viewModel);
        }
    }
}
