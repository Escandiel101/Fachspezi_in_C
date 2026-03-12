using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Numerics;

namespace ShopBackend.Infrastructure.Services
{
    public class OrderService : IOrderService
    {

        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }


        public async Task AddOrderItemAsync(int orderId, CreateOrderItemDto dto)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null) 
                throw new KeyNotFoundException($"Bestellung mit der ID: {orderId} nicht gefunden.");

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {dto.ProductId} nicht verfügbar.");

            var stock = await _context.Stocks
                .Where(s => s.ProductId == dto.ProductId)
                .FirstOrDefaultAsync();
            if (stock == null)
                throw new KeyNotFoundException("Kein Lagerbestand vorhanen.");
            if (stock.AvailableQuantity < dto.Quantity)
                throw new ArgumentException($"Es ist nur noch ein Lagerbestand von {stock.AvailableQuantity} vorhanden, bitte Bestellmenge anpassen.");

            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = product.Price,
                TaxRate = product.TaxRate,
            };

            _context.OrderItems.Add(orderItem);
            order.NetTotal += orderItem.LineTotal;
            order.GrossTotal += (orderItem.LineTotal + orderItem.TaxAmount);
            stock.ReservedQuantity += dto.Quantity;
            await _context.SaveChangesAsync();

        }


        public async Task<Order> CreateAsync(CreateOrderDto dto)
        {
            var order = new Order
            {
                CustomerId = dto.CustomerId,
                DiscountCodeId = dto.DiscountCodeId,
                Status = "ausstehend",
                NetTotal = 0,
                GrossTotal = 0,
                OrderDate = DateTime.UtcNow
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal totalTaxAmount = 0; // Prüfung kann auch innerhalb der foreach stattfinden, aber so ist es leichter ´prüfbar.
                                        // Alternativ vorher eine neue Liste orderItems erschaffen, im foreach befüllen und mit zweiter foreach drunter Net/Grosstotal setzen 
            foreach (var item in dto.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new KeyNotFoundException($"Produkt mit der ID: {item.ProductId} nicht gefunden.");

                var stock = await _context.Stocks
                    .Where(s => s.ProductId == item.ProductId)
                    .FirstOrDefaultAsync();

                if (stock == null)
                    throw new KeyNotFoundException("Kein Lagerbestand vorhanden.");

                if (stock.AvailableQuantity < item.Quantity)
                    throw new ArgumentException($"Es ist nur noch ein Lagerbestand von {stock.AvailableQuantity} vorhanden, bitte Bestellmenge anpassen.");

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    TaxRate = product.TaxRate,
                };

                _context.OrderItems.Add(orderItem);
                stock.ReservedQuantity += item.Quantity;
                order.NetTotal += orderItem.LineTotal;
                totalTaxAmount += orderItem.TaxAmount;
            }
            
            order.GrossTotal += totalTaxAmount;
            await _context.SaveChangesAsync();
            return order;
        }


        public async Task DeleteAsync(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");

              var invoice = await _context.Invoices
                .Where(i => i.OrderId == id)
                .FirstOrDefaultAsync();

            if (invoice != null || order.Status != "ausstehend")
                throw new ArgumentException($"Bestellung kann nicht gelöscht werden - Die Bestellung wurde bereits verarbeitet oder es existieren zugehörige Rechnungen");

                _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
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


        public async Task RemoveOrderItemAsync(int orderId, int orderItemId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {orderId} nicht gefunden.");

            var orderItem = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && oi.Id == orderItemId)
                .FirstOrDefaultAsync();
            if (orderItem == null)
                throw new KeyNotFoundException($"Bestellposition mit der ID: {orderItemId} nicht gefunden.");


            var invoice = await _context.Invoices
                .Where(i => i.OrderId == orderId)
                .FirstOrDefaultAsync();
            if (invoice != null || order.Status != "ausstehend")
                throw new ArgumentException($"Die Bestellposition kann nicht gelöscht werden - Die Bestellung wurde bereits verarbeitet oder es existieren zugehörige Rechnungen");


            var stock = await _context.Stocks
                .Where(s => s.ProductId == orderItem.ProductId)
                .FirstOrDefaultAsync();
            if (stock == null)
                throw new KeyNotFoundException("Datenbankfehler: Kein Lagerbestand für dieses Produkt - Löschung nicht möglich, bitte Administrator kontaktieren");

            if (stock.ReservedQuantity < orderItem.Quantity)
                throw new ArgumentException("Datenbankfehler: Interner Lagerbestandsfehler - Löschung nicht möglich, bitte Admininstrator kontaktieren");

            stock.ReservedQuantity -= orderItem.Quantity;

            order.NetTotal -= orderItem.LineTotal;
            order.GrossTotal -= (orderItem.LineTotal + orderItem.TaxAmount);
            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

           
        }


        public async Task UpdateAsync(int id, UpdateOrderDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");

            order.DiscountCodeId = dto.DiscountCodeId ?? order.DiscountCodeId;

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == id)
                .ToListAsync();

            if (dto.DiscountCodeId == null)
            {
                order.DiscountCodeId = null;

                order.NetTotal = 0;
                order.GrossTotal = 0;
                foreach (var item in orderItems)
                {
                    order.NetTotal += item.LineTotal;
                    order.GrossTotal += (item.LineTotal + item.TaxAmount);
                }
                // Alternativ mit LINQ ohne foreach und den variablen net/grossTotal:
                //order.NetTotal = orderItems.Sum(oi => oi.LineTotal);
                //order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);
            }
            else
            {
                var discountCode = await _context.DiscountCodes
                    .Where(dc => dc.Id == dto.DiscountCodeId)
                    .FirstOrDefaultAsync();
                if (discountCode == null)
                    throw new KeyNotFoundException($"Rabattcode mit der Id: {dto.DiscountCodeId} nicht gefunden.");
                if (discountCode.IsExpired)
                    throw new ArgumentException($"Rabattcode mit der Id: {dto.DiscountCodeId} ist abgelaufen.");
                if (!discountCode.HasStarted)
                    throw new ArgumentException($"Rabattcode mit der Id: {dto.DiscountCodeId} ist noch nicht gültig.");
                if (order.NetTotal < discountCode.MinOrderValue)
                    throw new ArgumentException($"Fehler: Der Mindestbestellwert für den Rabattcode liegt bei: {discountCode.MinOrderValue} EURO.");

            }       
           
        }


        public async Task CancelAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Stock)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");

            foreach (var item in order.OrderItems)
            {
                if (item.Product.Stock != null && item.Product.Stock.ReservedQuantity >= item.Quantity)
                    item.Product.Stock.ReservedQuantity -= item.Quantity;
            }

            order.Status = "storniert";
            await _context.SaveChangesAsync();
            

        }

        public Task UpdateOrderItemAsync(int orderId, int orderItemId, UpdateOrderItemDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
