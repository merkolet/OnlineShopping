using Confluent.Kafka;

namespace PaymentsService.Kafka
{
    public class KafkaProducerService : IKafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"]
            };
            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task ProduceAsync<TKey, TValue>(string topic, TKey key, TValue value)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key?.ToString(),
                    Value = System.Text.Json.JsonSerializer.Serialize(value)
                };
                
                _logger.LogInformation($"Producing message to topic '{topic}' with key '{message.Key}' and value '{message.Value}'");
                await _producer.ProduceAsync(topic, message);
                _logger.LogInformation($"[KafkaProducerService] Successfully produced message to topic '{topic}' with key '{message.Key}' and value: {message.Value}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error producing message to topic '{topic}'");
                throw;
            }
        }
    }
} 