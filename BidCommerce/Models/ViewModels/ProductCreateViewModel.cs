﻿using BidCommerce.Models;

namespace BidCommerce.ViewModels
{
    public class ProductCreateViewModel
    {
        public Product Product { get; set; } = new Product();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IFormFile ImageFile { get; set; }
        public string? AuctionDuration { get; set; } // e.g., "1 hour", "1 day", "3 days", etc.
        public IEnumerable<Condition> Condition { get; set; } = new List<Condition>();

    }
}
