using System;
using OrdersService.Contracts;

namespace OrdersService.Events
{
    public class PaymentStatusUpdatedEvent
    {
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
    }
} 