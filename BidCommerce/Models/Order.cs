using BidCommerce.Data;

namespace BidCommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public string SellerId { get; set; }
        public ApplicationUser Buyer { get; set; }
        public ApplicationUser Seller { get; set; }
        public long Amount { get; set; }
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public string Currency { get; set; } = "usd";
        public string? Description { get; set; }
        public string PaymentIntentId { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class OrderDetail
    {
        public int Id { get; set; }  // Primary key
        public int OrderId { get; set; }
        public Order Order { get; set; }  // Navigation property
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Succeeded,
        Failed,
        RequiresAction,
        Canceled
    }
}
