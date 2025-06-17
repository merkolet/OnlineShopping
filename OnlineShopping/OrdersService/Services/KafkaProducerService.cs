using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace OrdersService.Services
{
    public class KafkaProducerService : IKafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;

        public KafkaProducerService(IConfiguration configuration)
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                // Отключить idempotent producer для упрощения, так как Outbox уже обеспечивает идемпотентность
                EnableIdempotence = false 
            };

            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        }

        public async Task ProduceAsync(string topic, string message)
        {
                await _producer.ProduceAsync(topic, new Message<Null, string> { Value = message });
        }
    }
} 