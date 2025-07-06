using BidCommerce.Data;
using System.ComponentModel.DataAnnotations;

namespace BidCommerce.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public string ReviewerId { get; set; }
        public virtual ApplicationUser Reviewer { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
