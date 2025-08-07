using BidCommerce.Data;
using BidCommerce.Models;
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
        private readonly StackExchange.Redis.IDatabase _redis;

        public ChatHub(BidDb context, UserManager<ApplicationUser> userManager, StackExchange.Redis.IDatabase redis)
        {
            _context = context;
            _userManager = userManager;
            _redis = redis;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(message))
                return;

            var newMessage = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                SentAt = DateTime.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(newMessage);

            var chatKey = GetChatKey(senderId, receiverId);
            await _redis.ListRightPushAsync(chatKey, messageJson);

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
            await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, message);
        }

        public async Task GetMessageHistory(string receiverId)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(receiverId))
                return;

            var messages = await GetRedisMessagesAsync(senderId, receiverId);

            if (messages.Count == 0)
            {
                messages = GetSqlMessages(senderId, receiverId);
                await CacheMessagesToRedisAsync(messages);
            }

            messages = messages.OrderBy(m => m.SentAt).ToList();
            await Clients.Caller.SendAsync("ReceiveMessageHistory", messages);
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
            catch { /* Redis down */ }

            return messages;
        }

        private List<Message> GetSqlMessages(string senderId, string receiverId)
        {
            return _context.Messages
                .Where(m =>
                    (m.SenderId == senderId && m.ReceiverId == receiverId) ||
                    (m.SenderId == receiverId && m.ReceiverId == senderId))
                .OrderBy(m => m.SentAt)
                .ToList();
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
                catch { }
            }
        }

        private string GetChatKey(string userA, string userB)
        {
            var sorted = new[] { userA, userB }.OrderBy(id => id).ToArray();
            return $"chat:{sorted[0]}:{sorted[1]}";
        }

        public async Task ClearUserRedisMessages(string userId)
        {
            var endpoints = _redis.Multiplexer.GetEndPoints();
            var server = _redis.Multiplexer.GetServer(endpoints.First());

            var allKeys = server.Keys(pattern: "chat:*").ToList();
            var userKeys = allKeys
                .Where(key => key.ToString().Contains(userId))
                .ToList();

            foreach (var key in userKeys)
            {
                await _redis.KeyDeleteAsync(key);
            }
        }
    }
}
