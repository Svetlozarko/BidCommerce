namespace BidCommerce.Models.ViewModels
{
    public class OrderEvent
    {
        public string OrderId { get; set; } = "";
        public string BuyerId { get; set; } = "";
        public string SellerId { get; set; } = "";
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
