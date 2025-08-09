using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using BidCommerce.Models;
using BidCommerce.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BidCommerce.Services.RabbitMQ
{
    public class MessageConsumerService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageConsumerService> _logger;

        public MessageConsumerService(IServiceProvider serviceProvider, ILogger<MessageConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    UserName = "guest",
                    Password = "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: "chat_messages",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("MessageConsumerService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MessageConsumerService");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageJson = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<Message>(messageJson);

                    if (message != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<BidDb>();
                        var redis = scope.ServiceProvider.GetRequiredService<IDatabase>();

                        // Save to SQL
                        db.Messages.Add(message);
                        await db.SaveChangesAsync();

                        // Save to Redis
                        var chatKey = $"chat:{string.Join(":", new[] { message.SenderId, message.ReceiverId }.OrderBy(x => x))}";
                        await redis.ListRightPushAsync(chatKey, messageJson);

                        _logger.LogDebug($"Processed message from {message.SenderId} to {message.ReceiverId}");
                    }

                    // Acknowledge the message
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    // Reject the message and don't requeue it
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(
                queue: "chat_messages",
                autoAck: false,
                consumer: consumer);

            return Task.CompletedTask;
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
                _logger.LogError(ex, "Error disposing MessageConsumerService");
            }
            finally
            {
                base.Dispose();
            }
        }
    }
}
