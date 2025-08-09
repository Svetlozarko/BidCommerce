using BidCommerce.Interfaces;
using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BidCommerce.Services.RabbitMQ
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqPublisher> _logger;

        public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _logger = logger;

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = configuration.GetConnectionString("RabbitMQ:HostName") ?? "localhost",
                    Port = int.Parse(configuration.GetConnectionString("RabbitMQ:Port") ?? "5672"),
                    UserName = configuration.GetConnectionString("RabbitMQ:UserName") ?? "guest",
                    Password = configuration.GetConnectionString("RabbitMQ:Password") ?? "guest"
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _logger.LogInformation("RabbitMqPublisher initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMqPublisher");
                throw;
            }
        }

        public void Publish(string queueName, string message)
        {
            try
            {
                _channel.QueueDeclare(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true;

                _channel.BasicPublish(
                    exchange: "",
                    routingKey: queueName,
                    basicProperties: properties,
                    body: body);

                _logger.LogDebug($"Published message to queue: {queueName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish message to queue: {queueName}");
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
                _logger.LogError(ex, "Error disposing RabbitMqPublisher");
            }
        }
    }
}
