using System.ComponentModel.DataAnnotations;

namespace OrdersService.Models
{
    public class OutboxMessage
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime OccurredOn { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime? ProcessedDate { get; set; }
        public string? Error { get; set; }
    }
} 