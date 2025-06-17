using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;
using PaymentsService.Services;
using PaymentsService.Models.Events;
using Swashbuckle.AspNetCore.Annotations;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly PaymentsDbContext _db;
        private readonly OutboxService _outboxService;

        public AccountController(PaymentsDbContext db, OutboxService outboxService)
        {
            _db = db;
            _outboxService = outboxService;
        }

        [HttpPost("{userId}")]
        [SwaggerOperation(
            Summary = "Создать счет пользователя",
            Description = "Создает новый счет для указанного пользователя. У каждого пользователя может быть только один счет."
        )]
        public async Task<IActionResult> CreateAccount(string userId)
        {
            if (await _db.Accounts.AnyAsync(a => a.UserId == userId))
                return Conflict("Account already exists for this user");

            var account = new Account { UserId = userId, Balance = 0 };
            _db.Accounts.Add(account);

            await _outboxService.AddEventAsync("AccountCreated", new AccountCreatedEvent
            {
                UserId = account.UserId,
                InitialBalance = account.Balance
            });

            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpPost("{userId}/deposit")]
        [SwaggerOperation(
            Summary = "Пополнить счет пользователя",
            Description = "Пополняет баланс счета указанного пользователя на заданную сумму. Сумма должна быть положительной."
        )]
        public async Task<IActionResult> Deposit(string userId, [FromBody] decimal amount)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null) return NotFound();
            if (amount <= 0) return BadRequest("Amount must be positive");

            account.Balance += amount;

            await _outboxService.AddEventAsync("AccountDeposited", new AccountDepositedEvent
            {
                UserId = account.UserId,
                Amount = amount,
                NewBalance = account.Balance
            });

            await _db.SaveChangesAsync();
            return Ok(account);
        }

        [HttpGet("{userId}/balance")]
        [SwaggerOperation(
            Summary = "Просмотреть баланс счета пользователя",
            Description = "Возвращает текущий баланс счета для указанного пользователя."
        )]
        public async Task<IActionResult> GetBalance(string userId)
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (account == null) return NotFound();
            return Ok(new { account.UserId, account.Balance });
        }
    }
} 