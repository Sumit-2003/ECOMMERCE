using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;

public class DashboardController : Controller
{
    private readonly EcommerceContext _context;

    public DashboardController(EcommerceContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Total Sales
        var totalSales = await _context.Orders.SumAsync(o => o.TotalPrice);

        // Total Orders
        var totalOrders = await _context.Orders.CountAsync();

        // Sales by Category
        var salesByCategory = await _context.Orders
            .Join(
                _context.Products,
                order => order.ProductId,
                product => product.ProductId,
                (order, product) => new { product.Category.CategoryName, order.TotalPrice }
            )
            .GroupBy(op => op.CategoryName)
            .Select(g => new CategorySales
            {
                Category = g.Key,
                TotalSales = g.Sum(op => op.TotalPrice)
            })
            .ToListAsync();

        // User Count by City
        var userCountByCity = await _context.Users
            .GroupBy(u => u.City)
            .Select(g => new CityUserCount
            {
                City = g.Key,
                UserCount = g.Count()
            })
            .ToListAsync();

        // Transform to Dashboard Model
        var dashboardModel = new DashboardViewModel
        {
            TotalSales = totalSales,
            TotalOrders = totalOrders,
            SalesByCategory = salesByCategory,
            UserCountByCity = userCountByCity // Set the new property
        };

        return View(dashboardModel);
    }
}
