using Microsoft.EntityFrameworkCore;
using OrdersService.Data;
using OrdersService.Models;
using OrdersService.Contracts;
using OrdersService.Events;
using System.Text.Json;

namespace OrdersService.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrdersDbContext _context;
        private readonly IOutboxService _outboxService;

        public OrderService(OrdersDbContext context, IOutboxService outboxService)
        {
            _context = context;
            _outboxService = outboxService;
        }

        public async Task<Order> CreateOrder(CreateOrderRequest request)
        {
            var order = new Order
            {
                Id = request.OrderId,
                Status = OrderStatus.New,
                UserId = request.UserId,
                Amount = request.Amount,
                Description = request.Description
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderPaymentRequestedEvent = new OrderPaymentRequestedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount,
                Description = order.Description
            };
            await _outboxService.AddMessage(order.Id, "OrderPaymentRequestedEvent", JsonSerializer.Serialize(orderPaymentRequestedEvent));

            return order;
        }

        public async Task UpdateOrderStatus(Guid orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Order?> GetOrderById(Guid id)
        {
            return await _context.Orders.FindAsync(id);
        }
    }
} 