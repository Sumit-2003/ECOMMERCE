using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;

namespace NexsusEcommerce.Controllers
{
    public class ProductController : Controller
    {
        private readonly EcommerceContext _context;

        public ProductController(EcommerceContext context)
        {
            _context = context;
        }

  
        public IActionResult Index()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            return View(products);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductId,ProductName,Description,Price,StockQuantity,CategoryId,ImageData")] Product product, IFormFile image)
        {
            try
            {
                // Validate Price
                if (product.Price <= 1)
                {
                    ModelState.AddModelError("Price", "Price must be greater than 1.");
                }

                // Validate Stock Quantity
                if (product.StockQuantity <= 1)
                {
                    ModelState.AddModelError("StockQuantity", "Stock quantity must be greater than 1.");
                }

                // Validate Image
                if (image == null || image.Length == 0)
                {
                    ModelState.AddModelError("ImageData", "Product image is required.");
                }
                else
                {
                    // Validate image file size (optional, e.g., max 2MB)
                    if (image.Length > 2 * 1024 * 1024) // 2MB
                    {
                        ModelState.AddModelError("ImageData", "Image size cannot exceed 2MB.");
                    }

                    // Validate image file type (optional)
                    var supportedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                    if (!supportedTypes.Contains(image.ContentType))
                    {
                        ModelState.AddModelError("ImageData", "Only JPEG, PNG, and GIF images are supported.");
                    }
                }

                // Check if ModelState is valid before proceeding
                if (ModelState.IsValid)
                {
                    // Process and store the image if it is provided
                    if (image != null && image.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await image.CopyToAsync(ms);
                            product.ImageData = ms.ToArray();
                        }
                    }

                    // Save the product to the database
                    _context.Add(product);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                // Log the exception (optional)
                ModelState.AddModelError(string.Empty, "An error occurred while creating the product. Please try again.");
                // Optionally log the exception: _logger.LogError(ex, "Error creating product");
            }

            // If we reach here, something went wrong; redisplay the form with validation errors
            ViewBag.Categories = _context.Categories.ToList();
            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductId,ProductName,Description,Price,StockQuantity,CategoryId,ImageData")] Product product, IFormFile image)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (image != null && image.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await image.CopyToAsync(ms);
                            product.ImageData = ms.ToArray();
                        }
                    }
                    else
                    {
 
                        var existingProduct = await _context.Products.FindAsync(id);
                        if (existingProduct != null)
                        {
                            product.ImageData = existingProduct.ImageData;
                        }
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(p => p.ProductId == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

  
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
		public IActionResult Search(string query)
		{
			// Fetch products from the database
			var products = _context.Products.AsQueryable();

			if (!string.IsNullOrEmpty(query))
			{
				// Apply search filter
				products = products.Where(p => p.ProductName.Contains(query) || p.Description.Contains(query));
			}

			// Pass the filtered list to the view
			return View("Index", products.ToList());
		}

	}
}
