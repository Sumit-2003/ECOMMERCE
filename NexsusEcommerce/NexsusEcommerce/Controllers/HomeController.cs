using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NexsusEcommerce.Models;

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
                               .Select(p => new
                               {
                                   p.ProductId,
                                   p.ProductName,
                                   p.Description,
                                   p.ImageData,
                                   p.Price
                               })
                               .ToList();

        return PartialView("_ProductList", products);
    }

    [HttpPost]
    public IActionResult AddCartToDb(int id)
    {
        // Implement the logic to add the product to the cart
        // For now, just return a success response
        return Json(new { success = true, message = "Product added to cart successfully!" });
    }

    public IActionResult Search(string query, int? categoryId)
    {
        // Fetch products from the database
        var products = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(query))
        {
            // Apply search filter
            products = products.Where(p => p.ProductName.Contains(query) || p.Description.Contains(query));
        }

        if (categoryId.HasValue)
        {
            // Apply category filter
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        // Convert the results to a dynamic type
        var dynamicProducts = products.ToList().Select(p => new
        {
            p.ProductId,
            p.ProductName,
            p.Description,
            p.Price,
            ImageData = p.ImageData, // Assuming ImageData is a byte array
        });

        // Pass the dynamic list to the partial view
        return PartialView("_ProductList", dynamicProducts);
    }


}
