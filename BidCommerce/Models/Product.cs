using BidCommerce.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

        
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")] 
        public string Title { get; set; }

        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Starting price must be greater than 0.")]
        public decimal? StartingPrice { get; set; } 

        public bool IsBiddable { get; set; } 

        [Range(0.01, double.MaxValue, ErrorMessage = "Buy Now price must be greater than 0.")]
        public decimal? BuyNowPrice { get; set; }

        public decimal? CurrentBid { get; set; } = 0;

        public DateTime? BidEndTime { get; set; }

        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BindNever]
        public string? OwnerId { get; set; }

        [BindNever]
        [ForeignKey("OwnerId")]
        public ApplicationUser? Owner { get; set; }

        public List<ProductImage> Images { get; set; } = new List<ProductImage>();

        [NotMapped]
        public List<IFormFile>? ImageFiles { get; set; } // Up to 10 images


        public int? ConditionId { get; set; }

        [ForeignKey("ConditionId")]
        public Condition? Condition { get; set; }

        public int? StatusId { get; set; }
        [ForeignKey("StatusId")]
        public Status? Status { get; set; }

        public int Views { get; set; }
        public List<Bid>? Bids { get; set; } = new List<Bid>();
    }

}
