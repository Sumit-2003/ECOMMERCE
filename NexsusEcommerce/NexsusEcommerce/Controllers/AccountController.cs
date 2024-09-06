using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;
using NexsusEcommerce.Models.ViewModels; 
using NexsusEcommerce.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly EcommerceContext _context;
    private readonly PasswordHasher _passwordHasher;

    public AccountController(EcommerceContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher(); 
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

 
        //if (model.Password.Length < 8 || !IsStrongPassword(model.Password))
        //{
        //    ModelState.AddModelError("Password", "Password must be at least 8 characters long and strong.");
        //}


        if (ModelState.IsValid)
        {
            model.Password = _passwordHasher.HashPassword(model.Password); 
            _context.Users.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Registration successful!";
            return RedirectToAction("Login");
        }


        return View(model);
    }


    private bool IsStrongPassword(string password)
    {
        return password.Any(char.IsUpper) &&   
               password.Any(char.IsLower) &&   
               password.Any(char.IsDigit) &&  
               password.Any(c => !char.IsLetterOrDigit(c)); 
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
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

            if (user != null && _passwordHasher.VerifyHashedPassword(user.Password, model.Password))
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


  
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();


        TempData.Clear();


        return RedirectToAction("Login", "Account");
    }
}
