using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrdersService.Data;
using System.Text.Json;
using OrdersService.Models;
using Microsoft.EntityFrameworkCore;

namespace OrdersService.Services
{
    public class OutboxProcessorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxProcessorService> _logger;

        public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Service running.");
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var kafkaProducerService = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>();

                var messages = await dbContext.OutboxMessages
                    .Where(m => m.ProcessedDate == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        _logger.LogInformation("Processing outbox message: {MessageType}", message.Type);
                        await kafkaProducerService.ProduceAsync("order-payment-requests", message.Data);
                        message.ProcessedDate = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox message {MessageId}: {Error}", message.Id, ex.Message);
                        message.Error = ex.Message;
                    }
                }
                await dbContext.SaveChangesAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
} 