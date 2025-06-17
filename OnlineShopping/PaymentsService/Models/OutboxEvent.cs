using System;

namespace PaymentsService.Models
{
    public class OutboxEvent
    {
        public int Id { get; set; }
        public Guid EventId { get; set; }
        public string EventType { get; set; } = null!;
        public string Payload { get; set; } = null!;
        public DateTime? SentAt { get; set; }
    }
} 