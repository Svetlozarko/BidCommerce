using Microsoft.AspNetCore.Identity;
using BidCommerce.Models;

    namespace BidCommerce.Data
    {
        public class ApplicationUser : IdentityUser
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string NickName { get; set; }
            public int Age { get; set; }
            public string Country { get; set; }
            public string? PhotoFileName { get; set; }
            public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
            public int TotalProductsPosted { get; set; } = 0;
            public int TotalProductsSold { get; set; } = 0;
            public double AverageRating { get; set; } = 0.0;
            public int TotalRatingsCount { get; set; } = 0;
        public ICollection<Product> Products { get; set; }
        public string Description { get; set; } = string.Empty;

    }

}
