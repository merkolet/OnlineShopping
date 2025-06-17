namespace OrdersService.Services
{
    public interface IKafkaProducerService
    {
        Task ProduceAsync(string topic, string message);
    }
} 