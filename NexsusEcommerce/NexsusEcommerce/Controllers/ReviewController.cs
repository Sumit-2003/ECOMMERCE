using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models; // Update this based on your project
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;

namespace NexsusEcommerce.Controllers
{
    public class ReviewController : Controller
    {
        private readonly EcommerceContext _context;

        public ReviewController(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int id)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }


            // Fetch the product details by ProductId
            var product = await _context.Products
                .Include(p => p.Reviews) // Load reviews with the product
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            // Load existing reviews for the product
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt) // Optional: Order reviews by date
                .ToListAsync();

            // Load users associated with the reviews
            var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToListAsync();

            // Create a dictionary for quick look-up of usernames
            var userDictionary = users.ToDictionary(u => u.UserId, u => u.Username);

            // Pass the product, reviews, and users to the view
            var model = new ReviewViewModel
            {
                Product = product,
                Reviews = reviews,
                Users = users // Pass the list of users
            };

            return View(model);
        }


        [HttpPost]
        public IActionResult SubmitReview(Review review)
        {
            // Get the currently authenticated user's UserId from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId != null)
            {
                // Assign the UserId to the review
                review.UserId = userId;  // Assuming UserId is an integer
            }

            // Add the review to the database
            review.CreatedAt = DateTime.Now;
            _context.Reviews.Add(review);
            _context.SaveChanges();

            // Redirect back to the product review page after submitting the review
            return RedirectToAction("Index", new { id = review.ProductId });
        }


    }
}
