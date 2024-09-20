using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NexsusEcommerce.Models;
using System.Linq;
using System.Threading.Tasks;

namespace NexsusEcommerce.Controllers
{
    public class UserProfileController : Controller
    {
        private readonly EcommerceContext _context;

        public UserProfileController(EcommerceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Assume userId is retrieved from session or other means
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            // Fetch user data along with orders
            var user = await _context.Users
                .Include(u => u.Orders)
                .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Map user and orders to view model
            var viewModel = new UserProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                City = user.City,
                Orders = user.Orders.Select(o => new OrderViewModel
                {
                    OrderId=o.OrderId,
                    ProductId = o.ProductId,
                    ProductName = o.Product.ProductName,
                    Quantity = o.Quantity,
                    TotalPrice = o.TotalPrice,
                    OrderDate = o.OrderDate,
                    Description = o.Product.Description,
                    ImageData = o.Product.ImageData,
                    Status = o.Status // Map Status property
                }).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            // Find the order by ID
            var order = await _context.Orders.FindAsync(orderId);

            // Check if the order was found
            if (order == null)
            {
                // Return a 404 Not Found response if the order does not exist
                return NotFound();
            }

            // Check if the order can be cancelled (e.g., it is not already delivered)
            if (order.Status == "Delivered")
            {
                // Return a conflict status if the order cannot be cancelled
                return StatusCode(409, "Order cannot be cancelled as it has already been delivered.");
            }

            // Check if the order is already cancelled to prevent redundant updates
            if (order.Status == "Cancelled")
            {
                // Return a conflict status if the order is already cancelled
                return StatusCode(409, "Order is already cancelled.");
            }

            // Update the status of the order to "Cancelled"
            order.Status = "Cancelled";

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return a JSON response indicating success
            return  RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ReturnProduct(int orderId, string reason)
        {
            // Retrieve the userId from the session
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Unauthorized(new { Success = false, Message = "Unauthorized access." });
            }

            // Validate the reason provided
            if (string.IsNullOrWhiteSpace(reason))
            {
                return BadRequest(new { Success = false, Message = "Return reason is required." });
            }

            // Fetch the order to validate from the Orders table
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound(new { Success = false, Message = "Order not found." });
            }

            // Check if a return request already exists for this order
            var existingReturn = await _context.ReturnOrders
                .FirstOrDefaultAsync(r => r.OrderId == orderId && r.UserId == userId);

            if (existingReturn != null)
            {
                return Ok(new { Success = false, Message = "You have already sent a return request for this order." });
            }

            // Check if the order is within the return period (e.g., 7 days)
            var returnPeriod = TimeSpan.FromDays(7);
            if (order.OrderDate == null || (DateTime.Now - order.OrderDate.GetValueOrDefault()).TotalDays > returnPeriod.TotalDays)
            {
                return Ok(new { Success = false, Message = "Return period has expired." });
            }

            // Create a new return record
            var returnOrder = new ReturnOrder
            {
                OrderId = order.OrderId,
                ProductId = order.ProductId,
                UserId = order.UserId,
                Quantity = order.Quantity,
                ProductName = order.ProductName,
                Action = "Returned",
                CreatedAt = DateTime.Now,
                Reason = reason // Assign the reason provided by the user
            };

            _context.ReturnOrders.Add(returnOrder);
            await _context.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Product return processed successfully." });
        }

    }
}
