using System.ComponentModel.DataAnnotations;

namespace NexsusEcommerce.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "CAPTCHA is required")]
        public string CaptchaInput { get; set; } // New field for CAPTCHA input
    }
}

