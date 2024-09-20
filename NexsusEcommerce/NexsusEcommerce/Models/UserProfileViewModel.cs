namespace NexsusEcommerce.Models
{
    public class UserProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public List<OrderViewModel> Orders { get; set; }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? OrderDate { get; set; }
        public string? Description { get; set; }
        public byte[]? ImageData { get; set; }
        public string Status { get; set; } // Added Status property
    }
}
