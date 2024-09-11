using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexsusEcommerce.Models;
using Microsoft.AspNetCore.Authorization;

public class CouponController : Controller
{
    private readonly EcommerceContext _context;

    public CouponController(EcommerceContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var now = DateTime.Now;
        var coupons = _context.Coupons
            .Where(c => c.EndDate >= now)
            .ToList();
        return View(coupons);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Coupon coupon)
    {
        if (ModelState.IsValid)
        {
            coupon.CreatedAt = DateTime.Now;
            coupon.UpdatedAt = DateTime.Now;
            _context.Coupons.Add(coupon);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(coupon);
    }

    [HttpGet]
    public IActionResult Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var coupon = _context.Coupons.Find(id);
        if (coupon == null)
        {
            return NotFound();
        }

        return View(coupon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, [Bind("CouponID,CouponCode,Description,DiscountPercentage,StartDate,EndDate,CreatedAt,UpdatedAt")] Coupon coupon)
    {
        if (id != coupon.CouponId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                coupon.UpdatedAt = DateTime.Now;
                _context.Update(coupon);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponExists(coupon.CouponId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(coupon);
    }

    private bool CouponExists(int id)
    {
        return _context.Coupons.Any(e => e.CouponId == id);
    }

    public IActionResult Delete(int id)
    {
        var coupon = _context.Coupons.Find(id);
        if (coupon == null)
        {
            return NotFound();
        }
        return View(coupon);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var coupon = _context.Coupons.Find(id);
        _context.Coupons.Remove(coupon);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult ApplyCoupon(string couponCode)
    {
        var coupon = _context.Coupons.SingleOrDefault(c => c.CouponCode == couponCode && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now);
        if (coupon == null)
        {
            return Json(new { success = false, message = "Invalid or expired coupon." });
        }
        return Json(new { success = true, discount = coupon.DiscountPercentage });
    }


    [HttpPost("manual-cleanup")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManualCleanup()
    {
        var now = DateTime.Now;
        var expiredCoupons = await _context.Coupons
            .Where(c => c.EndDate < now)
            .ToListAsync();

        if (expiredCoupons.Any())
        {
            _context.Coupons.RemoveRange(expiredCoupons);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Expired coupons cleaned up successfully." });
        }

        return Ok(new { Message = "No expired coupons found to clean up." });
    }

}
