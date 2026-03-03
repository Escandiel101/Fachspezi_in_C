using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;


namespace ShopBackend.Application.Interfaces
{
    public interface IOrderService
    {

        Task<Order> GetOrderAsync(int id);
        Task<IEnumerable<Order>> GetAllOrdersAsync();
        Task<Order> CreateAsync(CreateOrderDto dto);
        Task UpdateAsync(int id, UpdateOrderDto dto);
        Task DeleteAsync(int id);


    }
}
