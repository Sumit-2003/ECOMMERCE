using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using static Azure.Core.HttpHeader;

public class CartController : Controller
{
    private readonly EcommerceContext _context;


    public CartController(EcommerceContext context)
    {
        _context = context;

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


        TempData["DiscountedTotal"] = discountedPrice.ToString("F2");

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


    [HttpPost]
    public IActionResult Checkout(string couponCode)
    {
        var userId = HttpContext.Session.GetInt32("UserId");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var coupon = _context.Coupons.FirstOrDefault(c => c.CouponCode == couponCode && c.EndDate >= DateTime.Now);

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

        if (coupon != null)
        {
            var couponVerification = new CouponVerification
            {
                UserId = (int)userId,
                CouponId = coupon.CouponId,
                CouponCode = coupon.CouponCode,
                VerificationDate = DateTime.Now,
                IsCouponUsed = true
            };

            _context.CouponVerifications.Add(couponVerification);

            var discount = coupon.DiscountPercentage / 100;
            totalPrice -= totalPrice * discount;
        }

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

            _context.Orders.Add(order);
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        _context.SaveChanges();


        var user = _context.Users.Find(userId);
        var confirmationViewModel = new OrderConfirmationViewModel
        {
            UserName = user.Username,
            Address = user.Address,
            Email = user.Email,
            TotalPrice = totalPrice,
            CartItems = cart.CartItems.Select(ci => new CartItemViewModel
            {
                ProductName = ci.Product.ProductName,
                Quantity = ci.Quantity,
                Price = ci.Product.Price,
                Total = ci.Quantity * ci.Product.Price
            }).ToList()
        };

        return View("OrderConfirmation", confirmationViewModel);
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



}