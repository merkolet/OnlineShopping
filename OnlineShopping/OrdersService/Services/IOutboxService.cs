namespace OrdersService.Services
{
    public interface IOutboxService
    {
        Task AddMessage(Guid entityId, string type, string data);
    }
} 