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
        public ICollection<Order> OrdersBought { get; set; }
        public ICollection<Order> OrdersSold { get; set; }
        public ICollection<Follower> Followers { get; set; } = new List<Follower>();

        // Users that this user follows
        public ICollection<Follower> Following { get; set; } = new List<Follower>();

        public int FollowersCount { get; set; } = 0;
        public int FollowingCount { get; set; } = 0;


    }

}
