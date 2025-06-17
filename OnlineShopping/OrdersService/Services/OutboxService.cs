using OrdersService.Data;
using OrdersService.Models;

namespace OrdersService.Services
{
    public class OutboxService : IOutboxService
    {
        private readonly OrdersDbContext _context;

        public OutboxService(OrdersDbContext context)
        {
            _context = context;
        }

        public async Task AddMessage(Guid entityId, string type, string data)
        {
            var message = new OutboxMessage
            {
                Id = entityId,
                OccurredOn = DateTime.UtcNow,
                Type = type,
                Data = data,
                ProcessedDate = null
            };

            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync();
        }
    }
} 