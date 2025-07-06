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
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal? StartingPrice { get; set; }

        public bool IsBiddable { get; set; } 

        [Range(0, double.MaxValue)]
        public decimal? BuyNowPrice { get; set; }

        public decimal? CurrentBid { get; set; } = 0;

        public DateTime? BidEndTime { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public ApplicationUser Owner { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }

}
