using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;

public class OrderController : Controller
{


    private readonly EcommerceContext _context;
    private readonly IEmailService _emailService;

    public OrderController(EcommerceContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // Existing SalesReport method
    public async Task<IActionResult> SalesReport()
    {
        var salesData = await _context.Orders
            .Join(
                _context.Users,
                order => order.UserId,
                user => user.UserId,
                (order, user) => new { order.OrderDate, user.City, order.TotalPrice }
            )
            .Where(o => o.OrderDate.HasValue) // Ensure OrderDate is not null
            .GroupBy(o => new { Date = o.OrderDate.Value.Date, Region = o.City })
            .Select(g => new
            {
                Date = g.Key.Date.ToString("yyyy-MM-dd"), // Formatting to match the JavaScript date format
                Region = g.Key.Region,
                TotalSales = g.Sum(o => o.TotalPrice)
            })
            .OrderBy(g => g.Date)
            .ThenBy(g => g.Region)
            .ToListAsync();

        return View(salesData);
    }

    // New method to get order details
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Product)
            .Where(o => o.OrderId == id)
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
    public async Task<IActionResult> AllOrders()
    {
        var orders = await _context.Orders
            .Include(o => o.Product) // Include related Product data
            .ToListAsync(); // Retrieve all orders

        return View(orders);
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int orderId, string status)
    {
        // Find the order by ID
        var order = await _context.Orders.FindAsync(orderId);

        // Check if the order was found
        if (order == null)
        {
            // Return a 404 Not Found response if the order does not exist
            return NotFound();
        }

        // Update the status of the order
        order.Status = status;

        // Save changes to the database
        await _context.SaveChangesAsync();

        // Return a JSON response indicating success
        // Return HTTP 200 OK with no content
        return Ok();
    }

    public async Task<IActionResult> AllReturnOrders()
    {
        var orders = await _context.ReturnOrders
            .Include(o => o.Product) // Include related Product data
            .ToListAsync(); // Retrieve all orders

        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmReturnOrder(int returnOrderId)
    {
        // Fetch the return order
        var returnOrder = await _context.ReturnOrders
            .Include(ro => ro.User) // Assuming User property exists
            .FirstOrDefaultAsync(ro => ro.ReturnOrderId == returnOrderId);

        if (returnOrder == null)
        {
            return NotFound("Return order not found.");
        }

        // Update the return order status to "Confirmed"
        returnOrder.Action = "Confirmed";
        await _context.SaveChangesAsync();

        // Send the confirmation email
        var user = await _context.Users.FindAsync(returnOrder.UserId);
        if (user != null)
        {
            string subject = "Return Request Confirmed";
            string body = $"Dear {user.FirstName} {user.LastName},<br><br>" +
                          "Your return request has been successfully accepted. " +
                          "The product will be picked up within 2 days.<br><br>" +
                          "Thank you for shopping with us.<br>" +
                          "Best regards,<br>" +
                          "NexsusEcommerce";

            await _emailService.SendEmailAsync(user.Email, subject, body);
        }

        // Redirect back to the list of return orders
        return RedirectToAction("AllReturnOrders");
    }


}
