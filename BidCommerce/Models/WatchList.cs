using BidCommerce.Data;

namespace BidCommerce.Models
{
    public class WatchlistItem
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

}
