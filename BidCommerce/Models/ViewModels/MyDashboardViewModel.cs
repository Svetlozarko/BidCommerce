using BidCommerce.Data;
using BidCommerce.Models;
using System.Collections.Generic;

namespace BidCommerce.ViewModels
{
    public class MyDashboardViewModel
    {
        public ApplicationUser CurrentUser { get; set; }
        public List<Product> ActiveListings { get; set; } = new List<Product>();
        public List<Product> PastListings { get; set; } = new List<Product>();
        public decimal TotalRevenue { get; set; }
        public int ActiveListingsCount { get; set; }
        public decimal ThisMonthRevenue { get; set; } = 0;
        public double Rating { get; set; } = 0.0;
        public int FollowersCount { get; set; } = 0;
    }
}
