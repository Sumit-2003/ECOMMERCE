using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DashboardController : Controller
{
    private readonly EcommerceContext _context;

    public DashboardController(EcommerceContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> Index1()
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
    public IActionResult Index(DateTime? startDate, DateTime? endDate, int categoryId = 0)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            startDate = DateTime.Today.AddDays(-7); // Default to past 7 days if no date range is provided
            endDate = DateTime.Today;
        }

        // Create a list to hold the sales data for each day
        var salesData = new List<DayWiseSales>();

        // Get all the dates in the range
        for (var date = startDate.Value.Date; date <= endDate.Value.Date; date = date.AddDays(1))
        {
            // Calculate the total sales for this particular day and category
            var totalSalesForDay = _context.Orders
                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == date &&
                            (categoryId == 0 || o.Product.CategoryId == categoryId)) // Ensure the category filter works
                .Sum(o => (decimal?)o.TotalPrice) ?? 0; // Safeguard against null

            // Add this date and the total sales to the sales data list
            salesData.Add(new DayWiseSales
            {
                Date = date,
                TotalSales = totalSalesForDay
            });
        }

        // Get the categories for the dropdown
        var categories = _context.Categories.ToList();

        // Define the baseline based on the selected category
        decimal baseline = 0;
        switch (categoryId)
        {
            case 1: // Electronics
                baseline = 150000;
                break;
            case 2: // Clothing
                baseline = 100000;
                break;
            case 3: // Groceries
                baseline = 100000;
                break;
            default:
                baseline = 50000; // Default baseline for other categories
                break;
        }

        // Prepare the ViewModel
        var viewModel = new DashboardViewModel
        {
            DayWiseSales = salesData,
            Categories = categories,
            SelectedCategoryId = categoryId,
            StartDate = startDate,
            EndDate = endDate,
            Baseline = baseline // Pass the baseline to the ViewModel
        };

        return View(viewModel);
    }

    //public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int categoryId = 0)
    //{
    //    var viewModel = new DashboardViewModel();

    //    // Common data fetching for dashboard statistics
    //    viewModel.TotalSales = await _context.Orders.SumAsync(o => o.TotalPrice);
    //    viewModel.TotalOrders = await _context.Orders.CountAsync();

    //    viewModel.SalesByCategory = await _context.Orders
    //        .Join(
    //            _context.Products,
    //            order => order.ProductId,
    //            product => product.ProductId,
    //            (order, product) => new { product.Category.CategoryName, order.TotalPrice }
    //        )
    //        .GroupBy(op => op.CategoryName)
    //        .Select(g => new CategorySales
    //        {
    //            Category = g.Key,
    //            TotalSales = g.Sum(op => op.TotalPrice)
    //        })
    //        .ToListAsync();

    //    viewModel.UserCountByCity = await _context.Users
    //        .GroupBy(u => u.City)
    //        .Select(g => new CityUserCount
    //        {
    //            City = g.Key,
    //            UserCount = g.Count()
    //        })
    //        .ToListAsync();

    //    // Handling day-wise sales data if dates are provided
    //    if (startDate.HasValue && endDate.HasValue)
    //    {
    //        var salesData = new List<DayWiseSales>();
    //        for (var date = startDate.Value.Date; date <= endDate.Value.Date; date = date.AddDays(1))
    //        {
    //            var totalSalesForDay = await _context.Orders
    //                .Where(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == date &&
    //                            (categoryId == 0 || _context.Products.Any(p => p.ProductId == o.ProductId && p.CategoryId == categoryId)))
    //                .SumAsync(o => (decimal?)o.TotalPrice) ?? 0;

    //            salesData.Add(new DayWiseSales
    //            {
    //                Date = date,
    //                TotalSales = totalSalesForDay
    //            });
    //        }

    //        viewModel.DayWiseSales = salesData;
    //        viewModel.StartDate = startDate;
    //        viewModel.EndDate = endDate;
    //        viewModel.SelectedCategoryId = categoryId;

    //        // Define the baseline based on the selected category
    //        viewModel.Baseline = categoryId switch
    //        {
    //            1 => 150000, // Electronics
    //            2 => 100000, // Clothing
    //            3 => 100000, // Groceries
    //            _ => 50000   // Default baseline
    //        };
    //    }
    //    else
    //    {
    //        viewModel.DayWiseSales = new List<DayWiseSales>();
    //        viewModel.Categories = await _context.Categories.ToListAsync();
    //    }

    //    return View("Index", viewModel); // Use a single view for both cases
    //}








    [HttpGet]
    public async Task<IActionResult> GetSalesData(DateTime startDate, DateTime endDate, int? category)
    {
        var salesByDate = await _context.Orders
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .Where(o => !category.HasValue || _context.Products
                .Where(p => p.CategoryId == category.Value)
                .Select(p => p.ProductId)
                .Contains(o.ProductId))
            .GroupBy(o => o.OrderDate.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                SalesAmount = g.Sum(o => o.TotalPrice)
            })
            .OrderBy(x => x.Date) // Order by date for the graph
            .ToListAsync();

        return Json(salesByDate);
    }
}
