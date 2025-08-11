using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using BidCommerce.Data;
using BidCommerce.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BidCommerce.Models.ViewModels;

namespace BidCommerce.Services.RabbitMQ
{
    public class OrderConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderConsumerService> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public OrderConsumerService(IServiceProvider serviceProvider, ILogger<OrderConsumerService> logger, IConfiguration config)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var factory = new ConnectionFactory()
            {
                HostName = config.GetConnectionString("RabbitMQ:HostName") ?? "localhost",
                UserName = config.GetConnectionString("RabbitMQ:UserName") ?? "guest",
                Password = config.GetConnectionString("RabbitMQ:Password") ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "order_processing", durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (sender, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var orderEvent = JsonSerializer.Deserialize<OrderEvent>(message);

                    if (orderEvent != null)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<BidDb>();

                        var order = new Order
                        {
                            Id = int.Parse(orderEvent.OrderId),
                            BuyerId = orderEvent.BuyerId,
                            SellerId = orderEvent.SellerId,
                            Amount = (long)orderEvent.TotalPrice,
                            CreatedAt = orderEvent.CreatedAt,
                            OrderDetails = orderEvent.Items.Select(item => new OrderDetail
                            {
                                ProductId = int.Parse(item.ProductId),
                                Quantity = item.Quantity,
                                Price = item.Price
                            }).ToList()
                        };

                        db.Orders.Add(order);
                        await db.SaveChangesAsync();

                        _logger.LogInformation($"Order {order.Id} persisted successfully.");
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order message");
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue: "order_processing", autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
