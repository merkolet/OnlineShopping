using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Models.Events;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly string _bootstrapServers;
        private readonly string _consumerGroup;
        private readonly string _orderPaymentRequestTopic;

        public KafkaConsumerService(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<KafkaConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            _consumerGroup = configuration["Kafka:ConsumerGroup"] ?? "payments-service-group";
            _orderPaymentRequestTopic = configuration["Kafka:OrderPaymentRequestTopic"] ?? "order-payment-requests";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Consumer Service started.");

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
            {
                consumer.Subscribe(_orderPaymentRequestTopic);
                _logger.LogInformation($"Subscribed to Kafka topic: {_orderPaymentRequestTopic}");

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = await Task.Run(() => consumer.Consume(TimeSpan.FromMilliseconds(100)), stoppingToken);

                            if (consumeResult == null)
                            {
                                // No message received within the timeout, so continue the loop
                                // and optionally introduce a small delay to avoid busy-waiting
                                await Task.Delay(100, stoppingToken); 
                                continue;
                            }

                            _logger.LogInformation($"Consumed message from topic {consumeResult.Topic} at offset {consumeResult.Offset.Value}: Key = {consumeResult.Message.Key}, Value = {consumeResult.Message.Value}");

                            // Parse the message value to get OrderId
                            var messageValue = JsonSerializer.Deserialize<OrderPaymentRequestedEvent>(consumeResult.Message.Value);
                            if (messageValue == null)
                            {
                                _logger.LogError("Failed to deserialize message value");
                                continue;
                            }

                            // Process the message using a new scope for InboxService
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var inboxService = scope.ServiceProvider.GetRequiredService<InboxService>();
                                await inboxService.ProcessOrderPaymentRequestAsync(
                                    messageValue.OrderId,
                                    "OrderPaymentRequested",
                                    consumeResult.Message.Value
                                );
                            }

                            // Manually commit offset after successful processing and transaction commit
                            consumer.Commit(consumeResult);
                            _logger.LogInformation($"Successfully processed and committed offset for message with OrderId: {messageValue.OrderId}");
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, $"Error consuming message from Kafka: {ex.Error.Reason}");
                            // Depending on the error, may need to seek to a specific offset or handle differently
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when cancellation token is triggered
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An unexpected error occurred while processing Kafka message.");
                            // Do not commit offset, so the message will be re-processed
                        }
                    }
                }
                finally
                {
                    consumer.Close();
                    _logger.LogInformation("Kafka Consumer closed.");
                }
            }

            _logger.LogInformation("Kafka Consumer Service stopped.");
        }
    }
} 