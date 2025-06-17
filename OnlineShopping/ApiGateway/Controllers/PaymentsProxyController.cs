using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Операции со счетами пользователей: создание счета, пополнение, просмотр баланса.")]
    public class PaymentsProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _paymentsBaseUrl = "http://payments-service:8080/api/Account";

        public PaymentsProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("{userId}")]
        [SwaggerOperation(Summary = "Создать счет пользователя", Description = "Создает новый счет для указанного пользователя. У каждого пользователя может быть только один счет.")]
        public async Task<IActionResult> CreateAccount([
            FromRoute,
            SwaggerParameter("userId", Required = true, Description = "Идентификатор пользователя")
        ] string userId)
        {
            var response = await _httpClient.PostAsync($"{_paymentsBaseUrl}/{userId}", null);
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var doc = JsonDocument.Parse(content);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpPost("{userId}/deposit")]
        [SwaggerOperation(Summary = "Пополнить счет пользователя", Description = "Пополняет баланс счета указанного пользователя на заданную сумму. Сумма должна быть положительной.")]
        public async Task<IActionResult> Deposit(string userId, [FromBody] decimal amount)
        {
            var json = JsonSerializer.Serialize(amount);
            var contentBody = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_paymentsBaseUrl}/{userId}/deposit", contentBody);
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var doc = JsonDocument.Parse(content);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet("{userId}/balance")]
        [SwaggerOperation(Summary = "Просмотреть баланс счета пользователя", Description = "Возвращает текущий баланс счета для указанного пользователя.")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            var response = await _httpClient.GetAsync($"{_paymentsBaseUrl}/{userId}/balance");
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var doc = JsonDocument.Parse(content);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            return StatusCode((int)response.StatusCode, content);
        }
    }
} 