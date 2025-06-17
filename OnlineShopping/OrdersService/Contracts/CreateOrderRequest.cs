namespace OrdersService.Contracts
{
    public class CreateOrderRequest
    {
        public required Guid OrderId { get; set; }
        public required string UserId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }
} 