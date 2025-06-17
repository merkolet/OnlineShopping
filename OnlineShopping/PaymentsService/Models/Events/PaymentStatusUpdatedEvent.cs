using PaymentsService.Models;

namespace PaymentsService.Models.Events
{
    public class PaymentStatusUpdatedEvent
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
    }
} 