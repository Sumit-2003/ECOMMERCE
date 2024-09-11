using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using System.Linq;
using System.Threading.Tasks;

public class OrderController : Controller
{
    private readonly EcommerceContext _context;

    public OrderController(EcommerceContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> SalesReport()
    {
        var salesData = await _context.Orders
            .Join(
                _context.Users,
                Order => Order.UserId,
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
}
