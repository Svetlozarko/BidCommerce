using BidCommerce.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace BidCommerce.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required, MaxLength(100)]
        public string Title { get; set; }

        [Required, MaxLength(1000)]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive.")]
        public decimal StartingPrice { get; set; }

        // If bidding is allowed
        public bool IsBiddable { get; set; }

        // Direct Buy Option (if allowed)
        public decimal? BuyNowPrice { get; set; }

        // Current highest bid (updated regularly)
        public decimal? CurrentBid { get; set; }

        // Bid end date
        public DateTime? BidEndTime { get; set; }

        // Upload multiple images per product (optional, store paths or use separate table)
        public string ImageUrl { get; set; }

        // Timestamp for auditing
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Link to the user who listed the product
        [ForeignKey("Owner")]
        public string OwnerId { get; set; }
        public virtual ApplicationUser Owner { get; set; }

        // Navigation: Bids placed on this product
        public virtual ICollection<Bid> Bids { get; set; }
    }
}
