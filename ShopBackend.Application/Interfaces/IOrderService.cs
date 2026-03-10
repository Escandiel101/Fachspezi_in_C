using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;


namespace ShopBackend.Application.Interfaces
{
    public interface IOrderService
    {

        Task<Order> GetByIdAsync(int id);
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order> CreateAsync(CreateOrderDto dto);
        Task UpdateAsync(int id, UpdateOrderDto dto);
        Task DeleteAsync(int id);
        Task CancelAsync(int id);

        Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId); // Da in den Bestellpositionen meist mehrere Produkte sind, braucht man IEnumerable als Aufzählungstyp
        // Wegen Enumerable ist der Getter hier mit dem Plural OrderItem"s" versehen
        Task AddOrderItemAsync(int orderId, CreateOrderItemDto dto);
        Task RemoveOrderItemAsync(int orderId, int orderItemId); // Ginge auch ohne OrderId, allerdings ist es so konsistenter und sicherer. 
        Task UpdateOrderItemAsync(int orderId, int orderItemId, UpdateOrderItemDto dto);

    }
}
