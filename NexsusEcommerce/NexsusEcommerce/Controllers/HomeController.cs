using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;

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
            ViewBag.Username = TempData["Username"];
            ViewBag.Email = TempData["Email"];
            ViewBag.MobileNumber = TempData["MobileNumber"];
            ViewBag.Address = TempData["Address"];
			

			return View();
        }

        public IActionResult Categories()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public IActionResult GetProductsByCategory(int categoryId)
        {
            var products = _context.Products
                                   .Where(p => p.CategoryId == categoryId)
                                   .Select(p => new {
                                       p.ProductId,
                                       p.ProductName,
                                       p.Description,
                                       p.ImageData,
                                       p.Price
                                   })
                                   .ToList();

            return PartialView("_ProductList", products);
        }



        


    }
}
