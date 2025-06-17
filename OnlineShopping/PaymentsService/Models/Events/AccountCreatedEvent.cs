namespace PaymentsService.Models.Events
{
    public class AccountCreatedEvent
    {
        public string UserId { get; set; } = null!;
        public decimal InitialBalance { get; set; }
    }
} 