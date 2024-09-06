namespace NexsusEcommerce.Models
{
    public class OrderConfirmationViewModel
    {
        public string UserName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public List<CartItemViewModel> CartItems { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
