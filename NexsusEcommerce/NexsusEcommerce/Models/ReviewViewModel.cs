namespace NexsusEcommerce.Models
{
    public class ReviewViewModel
    {
        public Product Product { get; set; }
        public List<Review> Reviews { get; set; }
        public Review NewReview { get; set; } = new Review(); // For review submission form
        public int? ProductId { get; internal set; }
        public int? UserId { get; internal set; }
        public int Rating { get; internal set; }
        public string ReviewText { get; internal set; }
        public List<User> Users { get; set; } // List of users to map UserId to Username
    }
}
