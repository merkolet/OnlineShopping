namespace OrdersService.Contracts
{
    public enum OrderStatus
    {
        New, // Сразу после создания заказа
        Processing, // Добавляем статус Processing
        Finished, // Если оплата прошла успешно
        Cancelled // Если оплата не удалась
    }
} 