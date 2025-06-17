using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Kafka;
using PaymentsService.Models;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class OutboxProcessorService : BackgroundService
    {
        private readonly ILogger<OutboxProcessorService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _paymentTopic;

        public OutboxProcessorService(
            ILogger<OutboxProcessorService> logger,
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _scopeFactory = scopeFactory;
            _paymentTopic = configuration["Kafka:PaymentTopic"] ?? throw new ArgumentNullException("Kafka:PaymentTopic");
            _logger.LogInformation($"Using Kafka topic: {_paymentTopic}");
            _logger.LogInformation($"Full Kafka configuration: {JsonSerializer.Serialize(configuration.GetSection("Kafka").GetChildren().ToDictionary(x => x.Key, x => x.Value))}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                        var kafkaProducer = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>();

                        var unsentEvents = await dbContext.OutboxEvents
                            .Where(e => e.SentAt == null)
                            .OrderBy(e => e.Id)
                            .Take(100) // Process in batches
                            .ToListAsync(stoppingToken);

                        foreach (var outboxEvent in unsentEvents)
                        {
                            try
                            {
                                if (outboxEvent.EventType != "PaymentStatusUpdated")
                                {
                                    _logger.LogInformation($"Skipping event {outboxEvent.EventId} of type {outboxEvent.EventType}, not for payment-status-updates topic.");
                                    outboxEvent.SentAt = DateTime.UtcNow; // Помечаем как отправленное, чтобы не зацикливать
                                    await dbContext.SaveChangesAsync(stoppingToken);
                                    continue;
                                }
                                await kafkaProducer.ProduceAsync(_paymentTopic, outboxEvent.EventId.ToString(), outboxEvent.Payload);
                                outboxEvent.SentAt = DateTime.UtcNow; // Mark as sent
                                await dbContext.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation($"Outbox event {outboxEvent.EventId} of type {outboxEvent.EventType} sent to Kafka.");
                            }
                            catch (ProduceException<string, string> ex)
                            {
                                _logger.LogError(ex, $"Failed to deliver message {outboxEvent.EventId} to Kafka: {ex.Error.Reason}");
                                // Depending on requirements, we might retry or move to a dead-letter queue
                                // For now, we'll leave it unsent to be retried in the next cycle.
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An unexpected error occurred while processing outbox event {outboxEvent.EventId}.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Outbox Processor Service loop.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Poll every 5 seconds
            }

            _logger.LogInformation("Outbox Processor Service stopped.");
        }
    }
} 