
namespace NexsusEcommerce.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordViewModel
    {
        public string Otp { get; set; } = null!;
        public string NewPassword { get; set; } = null!;

    }
}
