using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrdersService.Contracts;
using OrdersService.Data;
using OrdersService.Models;
using OrdersService.Events;

namespace OrdersService.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public KafkaConsumerService(
            IConsumer<string, string> consumer,
            ILogger<KafkaConsumerService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _consumer = consumer;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _logger.LogInformation("KafkaConsumerService constructor called");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaConsumerService started.");
            _consumer.Subscribe("payment-status-updates");
            _logger.LogInformation("Subscribed to topic 'payment-status-updates'.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Before _consumer.Consume");
                    var consumeResult = await Task.Run(() => _consumer.Consume(stoppingToken), stoppingToken);
                    _logger.LogInformation("After _consumer.Consume");
                    if (consumeResult == null)
                    {
                        await Task.Delay(100, stoppingToken);
                        continue;
                    }

                    try
                    {
                        _logger.LogInformation($"Received message from Kafka: {consumeResult.Message.Value}");
                        string payload = consumeResult.Message.Value;
                        using (var doc = JsonDocument.Parse(payload))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.String)
                            {
                                var inner = doc.RootElement.GetString();
                                _logger.LogInformation($"Kafka payload is string, inner: {inner}");
                                payload = inner ?? payload;
                            }
                            else
                            {
                                foreach (var prop in doc.RootElement.EnumerateObject())
                                {
                                    _logger.LogInformation($"Property: {prop.Name}, Value: {prop.Value}");
                                }
                            }
                        }
                        var paymentStatus = JsonSerializer.Deserialize<PaymentStatusUpdatedEvent>(payload);
                        _logger.LogInformation($"Deserialized payment status: {JsonSerializer.Serialize(paymentStatus)}");
                        if (paymentStatus == null)
                        {
                            _logger.LogWarning($"Deserialized PaymentStatusUpdatedEvent is null. Raw: {payload}");
                            continue;
                        }
                        _logger.LogInformation($"Deserialized PaymentStatusUpdatedEvent: OrderId={paymentStatus.OrderId}, Status={paymentStatus.Status}");
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                            await orderService.UpdateOrderStatus(paymentStatus.OrderId, paymentStatus.Status);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error deserializing message: {consumeResult.Message.Value} | Exception: {ex}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
            }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
} 