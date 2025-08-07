using BidCommerce.Data;
using System.ComponentModel.DataAnnotations;

namespace BidCommerce.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        public string SenderId { get; set; }
        public virtual ApplicationUser Sender { get; set; }

        public string ReceiverId { get; set; }
        public virtual ApplicationUser Receiver { get; set; }

        [Required]
        [StringLength(300, ErrorMessage = "Message content cannot exceed 300 characters.")]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }

}
