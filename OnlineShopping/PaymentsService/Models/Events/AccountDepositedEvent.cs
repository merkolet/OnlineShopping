namespace PaymentsService.Models.Events
{
    public class AccountDepositedEvent
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal NewBalance { get; set; }
    }
} 