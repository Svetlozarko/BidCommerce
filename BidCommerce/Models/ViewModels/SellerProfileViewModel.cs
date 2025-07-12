using BidCommerce.Data;
using BidCommerce.Models;
using System.Collections.Generic;

namespace BidCommerce.ViewModels
{
    public class SellerProfileViewModel
    {
        public ApplicationUser Seller { get; set; }
        public List<Product> CurrentListings { get; set; } = new List<Product>();

        // Simulated Seller Stats (these would typically come from a database or aggregated data)
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public string StatusBadge { get; set; } // e.g., "Top Seller", "Fast Shipper", "Verified"
        public string Description { get; set; }
        public int TotalSalesCount { get; set; }
        public int FollowersCount { get; set; }
        public double PositiveFeedbackPercentage { get; set; }
        public double ResponseRatePercentage { get; set; }
        public string ResponseTime { get; set; } // e.g., "< 1 hour"
        public int MemberSinceYear { get; set; }
        public string Location { get; set; }
        public bool IsVerifiedSeller { get; set; }
        public bool IsActiveDaily { get; set; }
    }
}
