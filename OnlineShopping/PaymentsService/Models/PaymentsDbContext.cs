using Microsoft.EntityFrameworkCore;

namespace PaymentsService.Models
{
    public class PaymentsDbContext : DbContext
    {
        public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<InboxEvent> InboxEvents { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }
    }
} 