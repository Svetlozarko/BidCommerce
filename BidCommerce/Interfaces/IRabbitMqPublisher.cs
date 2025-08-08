namespace BidCommerce.Interfaces
{
    public interface IRabbitMqPublisher
    {
        void Publish(string queueName, string message);
    }
}
