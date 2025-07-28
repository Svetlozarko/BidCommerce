using BidCommerce.Data;

namespace BidCommerce.Models
{
    public class Follower
    {
        public string FollowerId { get; set; }
        public ApplicationUser FollowerUser { get; set; }

        public string FollowedId { get; set; }
        public ApplicationUser FollowedUser { get; set; }

        public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    }

}
