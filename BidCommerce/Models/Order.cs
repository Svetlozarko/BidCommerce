namespace BidCommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }  
        public long Amount { get; set; }     
        public string Currency { get; set; } = "usd";
        public string? Description { get; set; }
        public string PaymentIntentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        RequiresAction,  // For 3D Secure or other user action required states
        Canceled
    }


}
