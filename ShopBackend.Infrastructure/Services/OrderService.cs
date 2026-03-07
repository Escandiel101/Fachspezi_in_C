using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;

namespace ShopBackend.Infrastructure.Services
{
    public class OrderService : IOrderService
    {

        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }


        public Task AddOrderItemAsync(int orderId, CreateOrderItemDto dto)
        {
            throw new NotImplementedException();
        }


        public async Task<Order> CreateAsync(CreateOrderDto dto)
        {
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                DiscountCodeId = dto.DiscountCodeId,
                Status = "pending",
                NetTotal = 0,
                GrossTotal = 0,
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }


        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");


        }



        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders.ToListAsync();
        }


        public async Task<Order> GetByIdAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");
            return order;
        }


        public async Task<IEnumerable<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {orderId} nicht gefunden.");

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();
            if (!orderItems.Any()) // if (orderitems == null) geht nicht, weil es ne fucking Liste ist... 
                throw new KeyNotFoundException($"Keine Bestellpositionen für die Bestellung mit der ID: {orderId} gefunden.");
            return orderItems;

        }


        public Task RemoveOrderItemAsync(int orderId, int orderItemId)
        {
            throw new NotImplementedException();
        }


        public Task UpdateAsync(int id, UpdateOrderDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
