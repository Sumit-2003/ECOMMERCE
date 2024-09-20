using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NexsusEcommerce.Models;
using System.Text;
using static Azure.Core.HttpHeader;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using System.IO;

using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Layout.Borders;

public class CartController : Controller
{

    private readonly EcommerceContext _context;
    private readonly IEmailService _emailService;

    // Single constructor accepting both dependencies
    public CartController(EcommerceContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public IActionResult AddCartToDb()
    {
        return View();
    }
    [HttpPost]
    public IActionResult AddCartToDb(int id)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {

            return Json(new { success = false, redirectToLogin = true, message = "Please log in first." });
        }

        try
        {
            var product = _context.Products.FirstOrDefault(s => s.ProductId == id);

            if (product != null && product.StockQuantity > 0)
            {
                var cart = _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefault(c => c.UserId == userId.Value);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId.Value,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    _context.SaveChanges();
                }

                var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == id);

                if (cartItem == null)
                {
                    cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        ProductId = id,
                        Quantity = 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(cartItem);
                }
                else
                {
                    cartItem.Quantity++;
                }

                var res = _context.SaveChanges();

                if (res > 0)
                {
                    return Json(new { success = true, message = "Product added to cart." });
                }
            }
            else
            {
                return Json(new { success = false, message = "Product not available." });
            }
        }
        catch (Exception e)
        {
            return Json(new { success = false, message = "Failed to add product to cart." });
        }

        return Json(new { success = false, message = "An unexpected error occurred." });
    }




    public async Task<IActionResult> Index()
    {

        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }


        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId.Value);

        if (cart == null)
        {
            return View("EmptyCart");
        }


        return View(cart);
    }
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int newQuantity)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }


        var cartItem = await _context.CartItems
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.CartItemId == cartItemId && c.Cart.UserId == userId.Value);

        if (cartItem != null && newQuantity > 0 && newQuantity <= cartItem.Product.StockQuantity)
        {
            cartItem.Quantity = newQuantity;
            _context.CartItems.Update(cartItem);

            await _context.SaveChangesAsync();

            TempData["CartMsg"] = "Quantity updated successfully!";
        }
        else if (newQuantity <= 0)
        {

            TempData["CartMsg"] = "Quantity must be at least 1.";
        }
        else
        {
            TempData["CartMsg"] = "Requested quantity exceeds available stock.";
        }

        return RedirectToAction("Index");
    }
    [HttpPost]
    public async Task<IActionResult> ApplyCoupon(string couponCode)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var cart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId.Value);

        if (cart == null)
        {
            return RedirectToAction("Index");
        }

        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.CouponCode == couponCode && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow);

        if (coupon == null)
        {
            TempData["CouponMsg"] = "Invalid or expired coupon code.";
            return RedirectToAction("Index");
        }

        var couponVerification = await _context.CouponVerifications
            .FirstOrDefaultAsync(cv => cv.UserId == userId.Value && cv.CouponCode == couponCode);

        if (couponVerification != null)
        {
            TempData["CouponMsg"] = "You have already used this coupon.";
            return RedirectToAction("Index");
        }

        var discount = coupon.DiscountPercentage / 100;

        var totalPrice = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
        var discountedPrice = totalPrice - (totalPrice * discount);

        // Store the discount in TempData
        TempData["DiscountedTotal"] = discountedPrice.ToString("F2");
        TempData["DiscountPercentage"] = coupon.DiscountPercentage.ToString("F0");

        var newCouponVerification = new CouponVerification
        {
            UserId = userId.Value,
            CouponId = coupon.CouponId,
            CouponCode = couponCode,
            VerificationDate = DateTime.UtcNow,
            IsCouponUsed = true
        };

        _context.CouponVerifications.Add(newCouponVerification);
        await _context.SaveChangesAsync();

        TempData["CouponMsg"] = "Coupon applied successfully!";
        return RedirectToAction("Index");
    }


    // Ensure you have this using directive

    public async Task<IActionResult> Checkout(string couponCode)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var cart = _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefault(c => c.UserId == userId);

        if (cart == null || cart.CartItems.Count == 0)
        {
            TempData["Message"] = "Your cart is empty or not found.";
            return RedirectToAction("Index", "Home");
        }

        decimal totalPrice = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);

        // Retrieve the discounted price and percentage from TempData
        var discountedTotal = TempData["DiscountedTotal"] as string;
        var discountPercentage = TempData["DiscountPercentage"] as string;

        if (discountedTotal != null)
        {
            totalPrice = decimal.Parse(discountedTotal);
        }
        else
        {
            // If no discount is applied, clear TempData values
            TempData.Remove("DiscountedTotal");
            TempData.Remove("DiscountPercentage");
        }

        var orders = new List<Order>();

        foreach (var item in cart.CartItems)
        {
            var order = new Order
            {
                UserId = (int)userId,
                ProductId = (int)item.ProductId,
                ProductName = item.Product.ProductName,
                Quantity = item.Quantity,
                TotalPrice = item.Quantity * item.Product.Price,
                OrderDate = DateTime.Now
            };

            orders.Add(order);
            _context.Orders.Add(order);
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        await _context.SaveChangesAsync();

        // Generate PDF Invoice
        var pdfBytes = GeneratePdfInvoice(userId.Value, orders, discountPercentage != null ? decimal.Parse(discountPercentage) : (decimal?)null, totalPrice);

        // Get user email
        var user = await _context.Users.FindAsync(userId.Value);
        var userEmail = user?.Email;

        if (!string.IsNullOrEmpty(userEmail))
        {
            // Send PDF Invoice via email
            await _emailService.SendEmailAsync(userEmail, "Your Invoice", "Please find your invoice attached.", pdfBytes, "invoice.pdf");
        }

        // Store the order ID in TempData to retrieve it later
        TempData["OrderId"] = orders.First().OrderId;
        TempData["UserId"] = userId.Value;

        return RedirectToAction("OrderConfirmation");
    }
    public IActionResult OrderConfirmation()
    {
        // You might need to set TempData here, or it could be set in the previous action
        return View();
    }

    public byte[] GeneratePdfInvoice(int userId, List<Order> orders, decimal? finalDiscount, decimal totalPrice)
    {
        using (var memoryStream = new MemoryStream())
        {
            var writer = new PdfWriter(memoryStream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Set font and margin
            var fontSize = 12f;

            // Title
            document.Add(new Paragraph("INVOICE")
                .SetFontSize(20)
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20));

            // Company Details
            document.Add(new Paragraph("NexsusEcommerce")
                .SetFontSize(16)
                .SetBold());

            document.Add(new Paragraph("Phone: +1 3649-6589")
                .SetFontSize(fontSize));

            document.Add(new Paragraph("Email: company@gmail.com")
                .SetFontSize(fontSize));

            document.Add(new Paragraph("Address: USA")
                .SetFontSize(fontSize));

            document.Add(new Paragraph("\n"));

            // User Details
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                document.Add(new Paragraph("Customer Details:")
                    .SetFontSize(14)
                    .SetBold());

                document.Add(new Paragraph($"Name: {user.Username}")
                    .SetFontSize(fontSize));

                document.Add(new Paragraph($"Email: {user.Email}")
                    .SetFontSize(fontSize));

                document.Add(new Paragraph($"Phone: {user.PhoneNumber}")
                    .SetFontSize(fontSize));

                document.Add(new Paragraph($"Address: {user.Address}")
                    .SetFontSize(fontSize));
            }

            document.Add(new Paragraph("\n"));

            // Invoice Details
            document.Add(new Paragraph($"Invoice #: {DateTime.Now.ToString("yyyyMMddHHmmss")}")
                .SetFontSize(14)
                .SetBold()
                .SetTextAlignment(TextAlignment.RIGHT));

            document.Add(new Paragraph("\n"));

            // Table for Order Details
            Table table = new Table(UnitValue.CreatePercentArray(new float[] { 4, 2, 2 }))
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetBorder(new SolidBorder(1))
                .SetMarginBottom(20);

            // Adding table headers with styling
            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Description").SetFontSize(fontSize))
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(new DeviceRgb(65, 65, 65)) // Dark gray background
                .SetFontColor(DeviceRgb.WHITE) // White text
                .SetBorder(new SolidBorder(1))); // Border around header cells

            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Quantity").SetFontSize(fontSize))
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(new DeviceRgb(65, 65, 65)) // Dark gray background
                .SetFontColor(DeviceRgb.WHITE) // White text
                .SetBorder(new SolidBorder(1))); // Border around header cells

            table.AddHeaderCell(new Cell()
                .Add(new Paragraph("Price").SetFontSize(fontSize))
                .SetBold()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBackgroundColor(new DeviceRgb(65, 65, 65)) // Dark gray background
                .SetFontColor(DeviceRgb.WHITE) // White text
                .SetBorder(new SolidBorder(1))); // Border around header cells

            // Adding table rows
            foreach (var order in orders)
            {
                table.AddCell(new Cell()
                    .Add(new Paragraph(order.ProductName))
                    .SetPadding(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBorder(new SolidBorder(1))); // Border around cells

                table.AddCell(new Cell()
                    .Add(new Paragraph(order.Quantity.ToString()))
                    .SetPadding(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBorder(new SolidBorder(1))); // Border around cells

                table.AddCell(new Cell()
                    .Add(new Paragraph($"₹{order.TotalPrice:F2}"))
                    .SetPadding(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetBorder(new SolidBorder(1))); // Border around cells
            }

            document.Add(table);

            // Summary
            document.Add(new Paragraph("\n"));

            decimal discountAmount = finalDiscount.HasValue ? (totalPrice * finalDiscount.Value / 100) : 0;
            decimal discountedPrice = totalPrice - discountAmount;

            document.Add(new Paragraph($"Total Amount: ₹{totalPrice:F2}")
                .SetFontSize(fontSize)
                .SetBold());

            if (discountAmount > 0)
            {
                document.Add(new Paragraph($"Discount ({finalDiscount:F0}%): ₹{discountAmount:F2}")
                    .SetFontSize(fontSize)
                    .SetBold());

                document.Add(new Paragraph($"Total Payable: ₹{discountedPrice:F2}")
                    .SetFontSize(fontSize)
                    .SetBold());
            }

            document.Add(new Paragraph("\n"));

            // Footer
            document.Add(new Paragraph($"Date: {DateTime.Now.ToShortDateString()}")
                .SetFontSize(fontSize));

            document.Add(new Paragraph("Thank you for shopping with us!")
                .SetFontSize(fontSize));

            document.Close();
            return memoryStream.ToArray();
        }
    }



    [HttpGet]
    public async Task<IActionResult> GetCoupons()
    {
        var coupons = await _context.Coupons.ToListAsync();
        return Json(coupons);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelectedItems(int[] selectedItems)
    {
        if (selectedItems == null || !selectedItems.Any())
        {
            return BadRequest("No items selected for deletion.");
        }

        var cartItemsToRemove = _context.CartItems
            .Where(ci => selectedItems.Contains(ci.CartItemId));

        _context.CartItems.RemoveRange(cartItemsToRemove);
        await _context.SaveChangesAsync();

        return Ok();
    }
    [HttpGet]
    public async Task<IActionResult> GetInvoice(int userId, int orderId)
    {
        // Retrieve the order details for generating the invoice
        var orders = await _context.Orders
            .Where(o => o.UserId == userId && o.OrderId == orderId)
            .ToListAsync();

        if (orders == null || !orders.Any())
        {
            return NotFound();
        }

        decimal totalPrice = orders.Sum(o => o.TotalPrice);

        // Extract CouponCode from TempData
        var couponCode = TempData["CouponCode"]?.ToString();

        // Retrieve discount percentage using the extracted couponCode
        var discountPercentage = await _context.Coupons
            .Where(c => c.CouponCode == couponCode && c.StartDate <= DateTime.UtcNow && c.EndDate >= DateTime.UtcNow)
            .Select(c => c.DiscountPercentage)
            .FirstOrDefaultAsync();

        var pdfBytes = GeneratePdfInvoice(userId, orders, discountPercentage != null ? decimal.Parse(discountPercentage.ToString()) : (decimal?)null, totalPrice);

        return File(pdfBytes, "application/pdf", "invoice.pdf");
    }

}
