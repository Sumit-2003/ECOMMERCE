using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;
using NexsusEcommerce.Models.ViewModels;
using NexsusEcommerce.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using Captcha.Core; // Ensure this is correct based on your CAPTCHA library
using System.Linq;
using System;
using Captcha.Net;
using NETCore.MailKit.Core;
using System.Net.Mail;

// If ICaptchaValidator is not available, use a fallback implementation
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, byte[] pdfBytes, string v);
    void SendEmailAsync(MailMessage emailMessage);
    Task SendEmailAsync(string email, string subject, string body);
}

public interface ICaptchaValidator
{
    bool Validate(string inputCaptcha, string sessionCaptcha);
}

public class CustomCaptchaValidator : ICaptchaValidator
{
    public bool Validate(string inputCaptcha, string sessionCaptcha)
    {
        return string.Equals(inputCaptcha, sessionCaptcha, StringComparison.OrdinalIgnoreCase);
    }
}

public class AccountController : Controller
{
    private readonly EcommerceContext _context;
    private readonly PasswordHasher _passwordHasher;
    private readonly ICaptchaValidator _captchaValidator;
    private readonly IEmailService _emailService;

    public AccountController(EcommerceContext context, IEmailService emailService)
    {
        _context = context;
        _passwordHasher = new PasswordHasher();
        _captchaValidator = new CustomCaptchaValidator(); // Use custom validator if needed
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(User model)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == model.Username);
        var emailUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (emailUser != null)
        {
            ModelState.AddModelError("Email", "This Email is already used.");
        }

        if (model.Password.Length < 8 || !IsStrongPassword(model.Password))
        {
            ModelState.AddModelError("Password", "Password must be at least 8 characters long and strong.");
        }

        if (ModelState.IsValid)
        {
            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            model.Otp = otp;
            model.OtpExpiration = DateTime.UtcNow.AddMinutes(2); // Set OTP expiration time
            model.EmailConfirmed = false;

            // Hash password and save user
            model.Password = _passwordHasher.HashPassword(model.Password);
            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // Send OTP email
            var subject = "Your OTP Code";
            var body = $"Your OTP code is {otp}. Please use this to verify your email. This code will expire in 2 minutes.";
            await _emailService.SendEmailAsync(model.Email, subject, body);

            // Redirect to OTP confirmation view
            TempData["UserId"] = model.UserId;
            return RedirectToAction("ConfirmEmail");
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ConfirmEmail()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmEmail(EmailConfirmationViewModel model)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == (int)TempData["UserId"] && u.Otp == model.Otp);

        if (user == null || user.OtpExpiration < DateTime.UtcNow)
        {
            TempData["ErrorMessage"] = "Invalid or expired OTP.";
            return RedirectToAction("ConfirmEmail");
        }

        // Confirm email and update user
        user.EmailConfirmed = true;
        user.Otp = null;
        user.OtpExpiration = null;
        await _context.SaveChangesAsync();



        TempData["SuccessMessage"] = "Email confirmed successfully!";
        return RedirectToAction("Login");
    }

    [HttpPost]
    public async Task<IActionResult> ResendOtp()
    {
        var userId = (int?)TempData["UserId"];

        if (userId == null)
        {
            TempData["ErrorMessage"] = "Session expired, please try registering again.";
            return RedirectToAction("Register");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Register");
        }

        // Generate new OTP
        var newOtp = new Random().Next(100000, 999999).ToString();
        user.Otp = newOtp;
        user.OtpExpiration = DateTime.UtcNow.AddMinutes(2);

        // Save the new OTP to the database
        await _context.SaveChangesAsync();

        // Send the new OTP email
        var subject = "Your Resent OTP Code";
        var body = $"Your new OTP code is {newOtp}. It will expire in 2 minutes.";
        await _emailService.SendEmailAsync(user.Email, subject, body);

        // Update TempData for OTP expiration
        TempData["OtpExpirationTime"] = user.OtpExpiration;

        TempData["SuccessMessage"] = "A new OTP has been sent to your email.";
        return RedirectToAction("ConfirmEmail");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string captchaInput)
    {
        var sessionCaptcha = HttpContext.Session.GetString("CaptchaCode");
        if (sessionCaptcha == null || !captchaInput.Equals(sessionCaptcha, StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.CaptchaError = "Captcha is incorrect. Please try again.";
            return View(model);
        }

        if (ModelState.IsValid)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == model.Username);

            if (admin != null && _passwordHasher.VerifyHashedPassword(admin.Password, model.Password))
            {
                HttpContext.Session.SetInt32("UserId", admin.AdminId);
                HttpContext.Session.SetString("UserType", "Admin");

                TempData["Username"] = admin.Username;
                TempData["Email"] = admin.Email;

                var coupons = await _context.Coupons
                    .Where(c => c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                    .Select(c => c.CouponCode)
                    .ToListAsync();

                if (coupons.Any())
                {
                    ViewBag.CouponCodes = coupons;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    });

                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user != null && user.EmailConfirmed && _passwordHasher.VerifyHashedPassword(user.Password, model.Password))
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);
                HttpContext.Session.SetString("MobileNumber", user.PhoneNumber);
                HttpContext.Session.SetString("Address", user.Address);
                HttpContext.Session.SetString("UserType", "User");

                TempData["Username"] = user.Username;
                TempData["Email"] = user.Email;
                TempData["MobileNumber"] = user.PhoneNumber;
                TempData["Address"] = user.Address;

                var coupons = await _context.Coupons
                    .Where(c => c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                    .Select(c => c.CouponCode)
                    .ToListAsync();

                if (coupons.Any())
                {
                    ViewBag.CouponCodes = coupons;
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid login attempt.");
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult GenerateCaptcha()
    {
        var captchaGenerator = new CaptchaGenerator();
        var captchaCode = captchaGenerator.GenerateCaptchaCode();
        var result = captchaGenerator.GenerateCaptchaImage(100, 50, captchaCode);
        HttpContext.Session.SetString("CaptchaCode", captchaCode);
        return File(result.CaptchaByteData, "image/png");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        TempData.Clear();
        return RedirectToAction("Login", "Account");
    }

    private bool IsStrongPassword(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            TempData["ErrorMessage"] = "No user found with that email address.";
            return RedirectToAction("ForgotPassword");
        }

        // Generate OTP
        var otp = new Random().Next(100000, 999999).ToString();
        user.ResetPasswordToken = otp;
        user.ResetPasswordTokenExpiration = DateTime.UtcNow.AddMinutes(10); // Token valid for 10 minutes

        await _context.SaveChangesAsync();

        // Send OTP email
        var subject = "Your Password Reset OTP";
        var body = $"Your OTP code for password reset is {otp}. It will expire in 10 minutes.";
        await _emailService.SendEmailAsync(user.Email, subject, body);

        TempData["SuccessMessage"] = "An OTP has been sent to your email.";
        return RedirectToAction("ResetPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.ResetPasswordToken == model.Otp && u.ResetPasswordTokenExpiration >= DateTime.UtcNow);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Invalid or expired OTP.";
            return RedirectToAction("ResetPassword");
        }

        if (ModelState.IsValid)
        {
            user.Password = _passwordHasher.HashPassword(model.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiration = null;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password has been reset successfully!";
            return RedirectToAction("Login");
        }

        return View(model);
    }



}