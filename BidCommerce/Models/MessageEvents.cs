using System.Text.Json.Serialization;

namespace BidCommerce.Models
{
    public class MessageEvent
    {
        public string EventType { get; set; } = "";
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Data { get; set; } = "";
    }

    public class NewMessageEvent
    {
        public string SenderId { get; set; } = "";
        public string ReceiverId { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; }
        public string MessageId { get; set; } = "";
    }

    public class CacheSyncEvent
    {
        public string UserId { get; set; } = "";
        public string ChatKey { get; set; } = "";
        public string Action { get; set; } = ""; // "sync", "clear", "update"
        public List<Message>? Messages { get; set; }
    }

    public class MessageHistoryRequest
    {
        public string SenderId { get; set; } = "";
        public string ReceiverId { get; set; } = "";
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public int Limit { get; set; } = 50;
    }
}
