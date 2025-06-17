using System;

namespace OrdersService.Events
{
    public class OrderPaymentRequestedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public DateTime Timestamp { get; set; }
    }
} 