using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Swashbuckle.AspNetCore.Annotations;
using ApiGateway.Models;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Операции с заказами: создание заказа, просмотр списка и статуса заказа.")]
    public class OrdersProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _ordersBaseUrl = "http://orders-service:8082/api/Orders";

        public OrdersProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Создает новый заказ", Description = "Создает новый заказ для пользователя. Оплата заказа происходит асинхронно через платежный сервис.")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var contentBody = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_ordersBaseUrl, contentBody);
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var doc = JsonDocument.Parse(content);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Получить список всех заказов", Description = "Возвращает список всех заказов пользователя.")]
        public async Task<IActionResult> GetAllOrders()
        {
            var response = await _httpClient.GetAsync(_ordersBaseUrl);
            var content = await response.Content.ReadAsStringAsync();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var doc = JsonDocument.Parse(content);
                content = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            return StatusCode((int)response.StatusCode, content);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Получить статус заказа по ID", Description = "Возвращает подробную информацию и статус заказа по его идентификатору.")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var response = await _httpClient.GetAsync($"{_ordersBaseUrl}/{id}");
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