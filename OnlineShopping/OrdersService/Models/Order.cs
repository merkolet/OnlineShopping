using System.ComponentModel.DataAnnotations;
using OrdersService.Contracts;

namespace OrdersService.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Description { get; set; } = null!;
        public OrderStatus Status { get; set; }
    }
} 