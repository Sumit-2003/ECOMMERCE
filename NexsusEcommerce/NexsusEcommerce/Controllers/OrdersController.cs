using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using NexsusEcommerce.Models;

public class OrdersController : Controller
{
    private readonly EcommerceContext _context;

    public OrdersController(EcommerceContext context)
    {
        _context = context;
    }

    public IActionResult OrderConfirmation()
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var orders = _context.Orders
            .Include(o => o.Product)  
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        var user = _context.Users.Find(userId);

        if (user == null)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["UserName"] = user.Username;
        ViewData["Address"] = user.Address;
        ViewData["Email"] = user.Email;
        ViewData["Orders"] = orders;

        return View();
    }

}
