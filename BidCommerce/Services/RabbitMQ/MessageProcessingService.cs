using BidCommerce.Data;
using BidCommerce.Models;
using BidCommerce.Interfaces;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace BidCommerce.Services.RabbitMQ
{
    public class MessageProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageProcessingService> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public MessageProcessingService(
            IServiceProvider serviceProvider,
            ILogger<MessageProcessingService> logger,
            IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Create separate connection for consumer
            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetConnectionString("RabbitMQ:HostName") ?? "localhost",
                Port = int.Parse(configuration.GetConnectionString("RabbitMQ:Port") ?? "5672"),
                UserName = configuration.GetConnectionString("RabbitMQ:UserName") ?? "guest",
                Password = configuration.GetConnectionString("RabbitMQ:Password") ?? "guest",
                VirtualHost = configuration.GetConnectionString("RabbitMQ:VirtualHost") ?? "/"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Processing Service started");

            // Set up consumers for different queues
            SetupMessageProcessingConsumer();
            SetupCacheSyncConsumer();
            SetupMessageHistoryConsumer();

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void SetupMessageProcessingConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var messageEvent = JsonSerializer.Deserialize<MessageEvent>(message);

                    if (messageEvent?.EventType == "new_message")
                    {
                        var newMessageEvent = JsonSerializer.Deserialize<NewMessageEvent>(messageEvent.Data);
                        await ProcessNewMessage(newMessageEvent!);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(ea.DeliveryTag, false, false); // Send to DLQ
                }
            };

            _channel.BasicConsume(
                queue: "message_processing",
                autoAck: false,
                consumer: consumer);
        }

        private void SetupCacheSyncConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var messageEvent = JsonSerializer.Deserialize<MessageEvent>(message);

                    if (messageEvent?.EventType == "cache_sync")
                    {
                        var cacheSyncEvent = JsonSerializer.Deserialize<CacheSyncEvent>(messageEvent.Data);
                        await ProcessCacheSync(cacheSyncEvent!);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing cache sync");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: "cache_sync",
                autoAck: false,
                consumer: consumer);
        }

        private void SetupMessageHistoryConsumer()
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var messageEvent = JsonSerializer.Deserialize<MessageEvent>(message);

                    if (messageEvent?.EventType == "message_history_request")
                    {
                        var historyRequest = JsonSerializer.Deserialize<MessageHistoryRequest>(messageEvent.Data);
                        await ProcessMessageHistoryRequest(historyRequest!);
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message history request");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: "message_history",
                autoAck: false,
                consumer: consumer);
        }

        private async Task ProcessNewMessage(NewMessageEvent messageEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BidDb>();
            var redis = scope.ServiceProvider.GetRequiredService<IDatabase>();

            try
            {
                // Save to SQL
                var message = new Message
                {
                    SenderId = messageEvent.SenderId,
                    ReceiverId = messageEvent.ReceiverId,
                    Content = messageEvent.Content,
                    SentAt = messageEvent.SentAt
                };

                context.Messages.Add(message);
                await context.SaveChangesAsync();

                // Cache to Redis
                var chatKey = GetChatKey(messageEvent.SenderId, messageEvent.ReceiverId);
                var messageJson = JsonSerializer.Serialize(message);
                await redis.ListRightPushAsync(chatKey, messageJson);

                // Set expiration for chat cache (optional)
                await redis.KeyExpireAsync(chatKey, TimeSpan.FromDays(30));

                _logger.LogDebug($"Processed new message from {messageEvent.SenderId} to {messageEvent.ReceiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process new message");
                throw;
            }
        }

        private async Task ProcessCacheSync(CacheSyncEvent syncEvent)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BidDb>();
            var redis = scope.ServiceProvider.GetRequiredService<IDatabase>();

            try
            {
                switch (syncEvent.Action.ToLower())
                {
                    case "sync":
                        await SyncMessagesToCache(context, redis, syncEvent.ChatKey);
                        break;
                    case "clear":
                        await redis.KeyDeleteAsync(syncEvent.ChatKey);
                        break;
                    case "update":
                        if (syncEvent.Messages != null)
                        {
                            await UpdateCacheWithMessages(redis, syncEvent.ChatKey, syncEvent.Messages);
                        }
                        break;
                }

                _logger.LogDebug($"Processed cache sync action: {syncEvent.Action} for key: {syncEvent.ChatKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process cache sync for key: {syncEvent.ChatKey}");
                throw;
            }
        }

        private async Task ProcessMessageHistoryRequest(MessageHistoryRequest request)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BidDb>();
            var redis = scope.ServiceProvider.GetRequiredService<IDatabase>();

            try
            {
                var chatKey = GetChatKey(request.SenderId, request.ReceiverId);

                // Try Redis first
                var messages = await GetMessagesFromRedis(redis, chatKey);

                if (messages.Count == 0)
                {
                    // Fallback to SQL
                    messages = await GetMessagesFromSql(context, request.SenderId, request.ReceiverId, request.Limit);

                    // Cache the results
                    if (messages.Count > 0)
                    {
                        await UpdateCacheWithMessages(redis, chatKey, messages);
                    }
                }

                _logger.LogDebug($"Retrieved {messages.Count} messages for chat between {request.SenderId} and {request.ReceiverId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message history request");
                throw;
            }
        }

        private async Task<List<Message>> GetMessagesFromRedis(IDatabase redis, string chatKey)
        {
            var messages = new List<Message>();
            try
            {
                var redisMessages = await redis.ListRangeAsync(chatKey);
                foreach (var msgJson in redisMessages)
                {
                    try
                    {
                        var msg = JsonSerializer.Deserialize<Message>(msgJson!);
                        if (msg != null) messages.Add(msg);
                    }
                    catch
                    {
                        // Skip malformed messages
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get messages from Redis for key: {chatKey}");
            }
            return messages;
        }

        private async Task<List<Message>> GetMessagesFromSql(BidDb context, string senderId, string receiverId, int limit)
        {
            return await context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId ||
                           m.SenderId == receiverId && m.ReceiverId == senderId)
                .OrderByDescending(m => m.SentAt)
                .Take(limit)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        private async Task SyncMessagesToCache(BidDb context, IDatabase redis, string chatKey)
        {
            // Extract user IDs from chat key
            var parts = chatKey.Split(':');
            if (parts.Length != 3) return;

            var user1 = parts[1];
            var user2 = parts[2];

            var messages = await GetMessagesFromSql(context, user1, user2, 100);
            await UpdateCacheWithMessages(redis, chatKey, messages);
        }

        private async Task UpdateCacheWithMessages(IDatabase redis, string chatKey, List<Message> messages)
        {
            // Clear existing cache
            await redis.KeyDeleteAsync(chatKey);

            // Add messages to cache
            foreach (var message in messages.OrderBy(m => m.SentAt))
            {
                var messageJson = JsonSerializer.Serialize(message);
                await redis.ListRightPushAsync(chatKey, messageJson);
            }

            // Set expiration
            await redis.KeyExpireAsync(chatKey, TimeSpan.FromDays(30));
        }

        private string GetChatKey(string userA, string userB)
        {
            var sorted = new[] { userA, userB }.OrderBy(id => id).ToArray();
            return $"chat:{sorted[0]}:{sorted[1]}";
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Close();
                _connection?.Close();
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MessageProcessingService");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
