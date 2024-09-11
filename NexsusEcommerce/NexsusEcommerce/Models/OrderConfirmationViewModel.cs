namespace NexsusEcommerce.Models
{
    public class OrderConfirmationViewModel
    {
        public string UserName { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public List<CartItemViewModel> CartItems { get; set; }
        public decimal TotalPrice { get; set; }

        public IEnumerable<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public IEnumerable<ProductViewModel> OrderedProducts { get; set; }
    }

    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
    }
}
