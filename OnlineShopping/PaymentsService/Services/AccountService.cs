using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Services
{
    public class AccountService
    {
        private readonly PaymentsDbContext _dbContext;
        private readonly ILogger<AccountService> _logger;

        public AccountService(PaymentsDbContext dbContext, ILogger<AccountService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Account> CreateAccountAsync(string userId)
        {
            var account = new Account { UserId = userId, Balance = 0 };
            await _dbContext.Accounts.AddAsync(account);
            _logger.LogInformation($"Account created for user {userId}.");
            return account;
        }

        public async Task<Account?> GetAccountAsync(string userId)
        {
            return await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        }

        public async Task<bool> DepositAsync(string userId, decimal amount)
        {
            if (amount <= 0)
            {
                _logger.LogWarning($"Deposit amount must be positive for user {userId}. Amount: {amount}");
                return false;
            }

            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
            {
                _logger.LogWarning($"Account not found for user {userId} for deposit.");
                return false;
            }

            account.Balance += amount;
            _logger.LogInformation($"Deposited {amount} to user {userId}'s account. New balance: {account.Balance}");
            return true;
        }

        public async Task<bool> DebitAccountAsync(string userId, decimal amount)
        {
            if (amount <= 0)
            {
                _logger.LogWarning($"Debit amount must be positive for user {userId}. Amount: {amount}");
                return false;
            }

            // Important: This needs to be atomic to prevent race conditions.
            // A more robust solution might involve:
            // 1. Using a row-level lock (e.g., SELECT ... FOR UPDATE in PostgreSQL).
            // 2. Implementing optimistic concurrency (e.g., using a Version column and checking it).
            // 3. Stored procedures for atomic updates.
            // For this example, we rely on EF Core's SaveChangesAsync to handle updates,
            // but true atomicity for concurrent debits on the same account
            // would require more specific database-level locking or optimistic concurrency.

            var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null)
            {
                _logger.LogWarning($"Account not found for user {userId} for debit.");
                return false;
            }

            if (account.Balance < amount)
            {
                _logger.LogWarning($"Insufficient funds for user {userId}. Balance: {account.Balance}, Attempted debit: {amount}");
                return false;
            }

            account.Balance -= amount;
            _logger.LogInformation($"Debited {amount} from user {userId}'s account. New balance: {account.Balance}");
            return true;
        }
    }
} 