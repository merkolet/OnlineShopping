using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;
using PaymentsService.Models.Events;
using System.Text.Json;

namespace PaymentsService.Services
{
    public class InboxService
    {
        private readonly PaymentsDbContext _dbContext;
        private readonly AccountService _accountService;
        private readonly OutboxService _outboxService;
        private readonly ILogger<InboxService> _logger;
        private readonly string _paymentTopic;

        public InboxService(
            PaymentsDbContext dbContext,
            AccountService accountService,
            OutboxService outboxService,
            IConfiguration configuration,
            ILogger<InboxService> logger)
        {
            _dbContext = dbContext;
            _accountService = accountService;
            _outboxService = outboxService;
            _logger = logger;
            _paymentTopic = configuration["Kafka:PaymentTopic"] ?? "payments-events";
        }

        public async Task ProcessOrderPaymentRequestAsync(Guid eventId, string eventType, string payload)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var inboxEvent = await _dbContext.InboxEvents.FirstOrDefaultAsync(e => e.EventId == eventId);

                if (inboxEvent != null)
                {
                    if (inboxEvent.ProcessedAt.HasValue)
                    {
                        _logger.LogInformation($"Event {eventId} of type {eventType} already processed. Skipping.");
                        return;
                    }
                    _logger.LogWarning($"Event {eventId} of type {eventType} found in inbox but not processed. Retrying.");
                }
                else
                {
                    inboxEvent = new InboxEvent
                    {
                        EventId = eventId,
                        EventType = eventType,
                        Payload = payload,
                        ProcessedAt = null
                    };
                    await _dbContext.InboxEvents.AddAsync(inboxEvent);
                    await _dbContext.SaveChangesAsync();
                }

                var orderPaymentRequested = JsonSerializer.Deserialize<OrderPaymentRequestedEvent>(payload);

                if (orderPaymentRequested == null)
                {
                    _logger.LogError($"Could not deserialize OrderPaymentRequestedEvent for event {eventId}.");
                    inboxEvent.ProcessedAt = DateTime.UtcNow;
                    _logger.LogInformation($"Adding Cancelled PaymentStatusUpdatedEvent to outbox for event {eventId} (deserialization failed).");
                    await _outboxService.AddEventAsync(
                        "PaymentStatusUpdated",
                        new PaymentStatusUpdatedEvent
                        {
                            OrderId = eventId,
                            Status = OrderStatus.Cancelled
                        }
                    );
                    _logger.LogInformation($"Successfully added Cancelled PaymentStatusUpdatedEvent to outbox for event {eventId}.");
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation($"Attempting to commit transaction for event {eventId} (deserialization failed).");
                    await transaction.CommitAsync();
                    _logger.LogInformation($"Transaction committed for event {eventId} (deserialization failed).");
                    return;
                }

                _logger.LogInformation($"Processing payment request for OrderId: {orderPaymentRequested.OrderId}, UserId: {orderPaymentRequested.UserId}, Amount: {orderPaymentRequested.Amount}");

                bool debitSuccess = await _accountService.DebitAccountAsync(orderPaymentRequested.UserId, orderPaymentRequested.Amount);

                if (debitSuccess)
                {
                    _logger.LogInformation($"Adding Finished PaymentStatusUpdatedEvent to outbox for OrderId: {orderPaymentRequested.OrderId}.");
                    await _outboxService.AddEventAsync(
                        "PaymentStatusUpdated",
                        new PaymentStatusUpdatedEvent
                        {
                            OrderId = orderPaymentRequested.OrderId,
                            Status = OrderStatus.Finished
                        }
                    );
                    _logger.LogInformation($"Successfully added Finished PaymentStatusUpdatedEvent to outbox for OrderId: {orderPaymentRequested.OrderId}.");
                    _logger.LogInformation($"Payment successful for OrderId: {orderPaymentRequested.OrderId}.");
                }
                else
                {
                    var reason = "Unknown reason";
                    var account = await _accountService.GetAccountAsync(orderPaymentRequested.UserId);
                    if (account == null) reason = "Account not found.";
                    else if (account.Balance < orderPaymentRequested.Amount) reason = "Insufficient funds.";

                    _logger.LogInformation($"Adding Cancelled PaymentStatusUpdatedEvent to outbox for OrderId: {orderPaymentRequested.OrderId} (payment failed).");
                    await _outboxService.AddEventAsync(
                        "PaymentStatusUpdated",
                        new PaymentStatusUpdatedEvent
                        {
                            OrderId = eventId,
                            Status = OrderStatus.Cancelled
                        }
                    );
                    _logger.LogInformation($"Successfully added Cancelled PaymentStatusUpdatedEvent to outbox for OrderId: {orderPaymentRequested.OrderId} (payment failed).");
                    _logger.LogWarning($"Payment failed for OrderId: {orderPaymentRequested.OrderId}. Reason: {reason}");
                }

                inboxEvent.ProcessedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Attempting to commit transaction for event {eventId} (processing completed).");
                await transaction.CommitAsync();
                _logger.LogInformation($"Transaction committed for event {eventId} (processing completed).");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error processing event {eventId} of type {eventType}. Transaction rolled back.");
                throw;
            }
        }
    }
} 