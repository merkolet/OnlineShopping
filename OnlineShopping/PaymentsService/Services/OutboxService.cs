using PaymentsService.Models;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class OutboxService
    {
        private readonly PaymentsDbContext _dbContext;
        private readonly ILogger<OutboxService> _logger;

        public OutboxService(PaymentsDbContext dbContext, ILogger<OutboxService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task AddEventAsync<T>(string eventType, T payload)
        {
            var outboxEvent = new OutboxEvent
            {
                EventId = Guid.NewGuid(),
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload),
                SentAt = null // Will be set when sent to Kafka
            };

            await _dbContext.OutboxEvents.AddAsync(outboxEvent);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Event of type '{eventType}' added to outbox.");
        }
    }
} 