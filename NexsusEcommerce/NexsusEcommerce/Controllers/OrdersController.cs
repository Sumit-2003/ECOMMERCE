using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using System.Linq;
using System.Threading.Tasks;

public class OrdersController : Controller
{
    private readonly EcommerceContext _context;

    public OrdersController(EcommerceContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OrderConfirmation()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = await _context.Orders
            .Include(o => o.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["UserName"] = user.Username;
        ViewData["Address"] = user.Address;
        ViewData["Email"] = user.Email;
        ViewData["Orders"] = orders;

        // Prepare sales data for chart
        var salesData = orders
            .GroupBy(o => o.OrderDate.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalSales = g.Sum(o => o.TotalPrice)
            })
            .OrderBy(g => g.Date)
            .ToList();

        ViewData["SalesData"] = salesData;

        return View();
    }
}
