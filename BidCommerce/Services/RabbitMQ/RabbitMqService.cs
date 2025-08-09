using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using BidCommerce.Interfaces;

namespace BidCommerce.Services.RabbitMQ
{
    public class RabbitMqService : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqService> _logger;

        public RabbitMqService(IConfiguration configuration, ILogger<RabbitMqService> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = configuration.GetConnectionString("RabbitMQ:HostName") ?? "localhost",
                Port = int.Parse(configuration.GetConnectionString("RabbitMQ:Port") ?? "5672"),
                UserName = configuration.GetConnectionString("RabbitMQ:UserName") ?? "guest",
                Password = configuration.GetConnectionString("RabbitMQ:Password") ?? "guest",
                VirtualHost = configuration.GetConnectionString("RabbitMQ:VirtualHost") ?? "/"
            };

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Declare queues
                DeclareQueues();

                _logger.LogInformation("RabbitMQ connection established successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish RabbitMQ connection");
                throw;
            }
        }

        private void DeclareQueues()
        {
            // Message processing queue
            _channel.QueueDeclare(
                queue: "message_processing",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Cache sync queue
            _channel.QueueDeclare(
                queue: "cache_sync",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Message history queue
            _channel.QueueDeclare(
                queue: "message_history",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Dead letter queue for failed messages
            _channel.QueueDeclare(
                queue: "message_dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        public void Publish(string queueName, string message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message);
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug($"Published message to queue {queueName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish message to queue {queueName}");
                throw;
            }
        }

        public void Dispose()
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
                _logger.LogError(ex, "Error disposing RabbitMqService");
            }
        }
    }
}
