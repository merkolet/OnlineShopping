namespace ApiGateway.Models
{
    public class CreateOrderRequest
    {
        public Guid OrderId { get; set; }
        public string UserId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
} 