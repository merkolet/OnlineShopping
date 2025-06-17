using OrdersService.Models;
using OrdersService.Contracts;

namespace OrdersService.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrder(CreateOrderRequest request);
        Task UpdateOrderStatus(Guid orderId, OrderStatus status);
        Task<IEnumerable<Order>> GetAllOrders();
        Task<Order?> GetOrderById(Guid id);
    }
} 