using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Text.Json;

namespace BidCommerce.Services
{
    public class ChatHub : Hub
    {
        private readonly BidDb _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDatabase _redis;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            BidDb context,
            UserManager<ApplicationUser> userManager,
            IDatabase redis,
            IRabbitMqPublisher rabbitMqPublisher,
            ILogger<ChatHub> logger)
        {
            _context = context;
            _userManager = userManager;
            _redis = redis;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                // Create message event for RabbitMQ
                var newMessageEvent = new NewMessageEvent
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    MessageId = Guid.NewGuid().ToString()
                };

                var messageEvent = new MessageEvent
                {
                    EventType = "new_message",
                    Data = JsonSerializer.Serialize(newMessageEvent)
                };

                // Publish to RabbitMQ for async processing
                var eventJson = JsonSerializer.Serialize(messageEvent);
                _rabbitMqPublisher.Publish("message_processing", eventJson);

                // Send real-time notifications via SignalR
                await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
                await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, message);

                _logger.LogDebug($"Message sent from {senderId} to {receiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message");
                await Clients.Caller.SendAsync("MessageError", "Failed to send message");
            }
        }

        public async Task GetMessageHistory(string receiverId)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                return;

            try
            {
                var messages = await GetRedisMessagesAsync(senderId, receiverId);
                if (messages.Count == 0)
                {
                    messages = GetSqlMessages(senderId, receiverId);
                    await CacheMessagesToRedisAsync(messages);
                }

                messages = messages.OrderBy(m => m.SentAt).ToList();
                await Clients.Caller.SendAsync("ReceiveMessageHistory", messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get message history");
                await Clients.Caller.SendAsync("MessageError", "Failed to load message history");
            }
        }

        private async Task<List<Message>> GetRedisMessagesAsync(string senderId, string receiverId)
        {
            var key = GetChatKey(senderId, receiverId);
            var messages = new List<Message>();
            try
            {
                var redisMessages = await _redis.ListRangeAsync(key);
                foreach (var msgJson in redisMessages)
                {
                    try
                    {
                        var msg = JsonSerializer.Deserialize<Message>(msgJson!);
                        if (msg != null) messages.Add(msg);
                    }
                    catch { /* Ignore bad JSON */ }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get messages from Redis for key: {key}");
            }
            return messages;
        }

        private List<Message> GetSqlMessages(string senderId, string receiverId)
        {
            try
            {
                return _context.Messages
                    .Where(m =>
                        (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                        (m.SenderId == receiverId && m.ReceiverId == senderId))
                    .OrderByDescending(m => m.SentAt)
                    .Take(50)
                    .OrderBy(m => m.SentAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get messages from SQL");
                return new List<Message>();
            }
        }

        private async Task CacheMessagesToRedisAsync(List<Message> messages)
        {
            if (messages.Count == 0) return;

            var key = GetChatKey(messages[0].SenderId, messages[0].ReceiverId);
            foreach (var msg in messages)
            {
                try
                {
                    var json = JsonSerializer.Serialize(msg);
                    await _redis.ListRightPushAsync(key, json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache message to Redis");
                }
            }
        }

        private string GetChatKey(string userA, string userB)
        {
            var sorted = new[] { userA, userB }.OrderBy(id => id).ToArray();
            return $"chat:{sorted[0]}:{sorted[1]}";
        }
    }
}
