using BidCommerce.Data;
using BidCommerce.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BidCommerce.Models
{
    public class Bid
    {
        [Key]
        public int BidId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Product")]
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [ForeignKey("Bidder")]
        public string BidderId { get; set; }
        public virtual ApplicationUser Bidder { get; set; }
    }
}
