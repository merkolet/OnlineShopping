namespace PaymentsService.Models.Events
{
    public class OrderPaymentRequestedEvent
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 