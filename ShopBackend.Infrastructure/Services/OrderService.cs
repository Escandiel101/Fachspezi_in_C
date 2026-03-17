using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Diagnostics.Tracing;
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
                throw new KeyNotFoundException("Kein Lagerbestand vorhanden.");
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

            await RecalculateDiscountAsync(order);
            stock.ReservedQuantity += dto.Quantity;
            await _context.SaveChangesAsync();

        }


        public async Task<Order> CreateAsync(CreateOrderDto dto)
        {   // Neu Transaktionen. Ohne diese würde im Falle einer Exception unten bei den ganzen ifs durch das erste SaveChangesAsync(); nach dem kreieren der order 
            // praktisch eine verwaiste leere orderliste existieren, da sie hier ja bereits gespeichert wird. Save einfach weglassen?!
            // Nein! Ohne das Save gibt es in der Db keine orderId, die ich für die OrderItems aber zwingend brauche.. -> Lösung Transaktionen try/catch rollback bei exception.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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
                    order.GrossTotal += orderItem.LineTotal + orderItem.TaxAmount;

                }

                if (dto.DiscountCodeId != null)
                {
                    // order.DiscountCodeId = dto.DiscountCodeId redundant, da  oben bereits gesetzt 
                    await ApplyDiscountAsync(order, dto.DiscountCodeId.Value);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
                
            catch
            { 
                await transaction.RollbackAsync();
                throw;
            }

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

            // Neue Hilfsfunktion:
            await ClearDiscountAsync(order);

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
            
            // Neue Umsetzung wie bei UpdateOrderItemAsync... damit es keinen Fehler mit doppeltem Rabatt beim Abzug wie vorher in den alten Berechnungen mit -= geben kann.
            _context.OrderItems.Remove(orderItem);

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId && _context.Entry(oi).State != EntityState.Deleted)
                .ToListAsync();
            order.NetTotal = orderItems.Sum(oi => oi.LineTotal);
            order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);

            await RemoveDiscountIfInvalidAsync(order);
            await _context.SaveChangesAsync();
        }


        public async Task UpdateAsync(int id, UpdateOrderDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                    throw new KeyNotFoundException($"Bestellung mit der ID: {id} nicht gefunden.");

                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == id)
                    .ToListAsync();

                if (dto.DiscountCodeId == null)
                {
                    await ClearDiscountAsync(order);
                    // Alternativ mit LINQ ohne foreach und den variablen net/grossTotal:
                    //order.NetTotal = orderItems.Sum(oi => oi.LineTotal);
                    //order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);
                    order.NetTotal = 0;
                    order.GrossTotal = 0;
                    foreach (var item in orderItems)
                    {
                        order.NetTotal += item.LineTotal;
                        order.GrossTotal += (item.LineTotal + item.TaxAmount);
                    }
                }
                else
                {
                    // Neu mal mit LINQ:
                    order.NetTotal = orderItems.Sum(oi => oi.LineTotal);
                    order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);

                    await ClearDiscountAsync(order);

                    order.DiscountCodeId = dto.DiscountCodeId;
                    if (dto.DiscountCodeId != null)
                        await ApplyDiscountAsync(order, dto.DiscountCodeId.Value);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelAsync(int id)
        {
            var order = await _context.Orders
                // JOIN ähnlicher Test obs auch so läuft, statt mit vielen var x =... zu arbeiten.
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
            await ClearDiscountAsync(order);
            await _context.SaveChangesAsync();
        }


        public async Task UpdateOrderItemAsync(int orderId, int orderItemId, UpdateOrderItemDto dto)
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
                throw new ArgumentException($"Die Bestellung wurde bereits verarbeitet und kann nicht mehr geändert werden.");

            var stock = await _context.Stocks
                .Where(s => s.ProductId == orderItem.ProductId)
                .FirstOrDefaultAsync();
            if (stock == null)
                throw new KeyNotFoundException("Datenbankfehler: Kein Lagerbestand für dieses Produkt gefunden - Änderung nicht möglich, bitte Administrator kontaktieren");

            if (dto.Quantity == null || dto.Quantity < 0)
                throw new ArgumentException("Bestellmenge muss größer oder gleich 0 sein (0 für entfernen).");
            if (dto.Quantity == 0)
            {
                await RemoveOrderItemAsync(orderId, orderItemId);
                return;
            }

            int newQuantity = dto.Quantity.Value;
            if (stock.AvailableQuantity < newQuantity - orderItem.Quantity)
                throw new ArgumentException($"Verfügbarer Lagerbestand: {stock.AvailableQuantity} nicht ausreichend für eine Erhöhung auf eine Menge von: {dto.Quantity}");

            stock.ReservedQuantity += newQuantity - orderItem.Quantity;
            orderItem.Quantity = newQuantity;

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            order.NetTotal = orderItems.Sum(oi => oi.LineTotal);
            order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);

            await RemoveDiscountIfInvalidAsync(order);
            await RecalculateDiscountAsync(order);

            await _context.SaveChangesAsync();

        }



        // Hilfsfunktionen:


        private async Task ApplyDiscountAsync(Order order, int discountCodeId)
        {
            var discountCode = await _context.DiscountCodes.FindAsync(discountCodeId);
            
            if (discountCode == null)
                throw new KeyNotFoundException($"Rabattcode mit der Id: {discountCodeId} nicht gefunden.");
            if (discountCode.IsExpired)
                throw new ArgumentException($"Rabattcode mit der Id: {discountCodeId} ist abgelaufen.");
            if (!discountCode.HasStarted)
                throw new ArgumentException($"Rabattcode mit der Id: {discountCodeId} ist noch nicht gültig.");
            if (order.NetTotal < discountCode.MinOrderValue)
                throw new ArgumentException($"Fehler: Der Mindestbestellwert für den Rabattcode liegt bei: {discountCode.MinOrderValue} EURO.");

            decimal discount = discountCode.DiscountPercentage / 100m;
            order.NetTotal = order.NetTotal * (1 - discount);
            order.GrossTotal = order.GrossTotal * (1 - discount);

           discountCode.UsedCount++;
        }


        private async Task RemoveDiscountIfInvalidAsync(Order order, bool recalculate = false)
        {
            if (order.DiscountCodeId == null) 
                return;

            var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId);

            if (discountCode == null)
                throw new KeyNotFoundException($"Rabattcode mit der ID: {order.DiscountCodeId} nicht gefunden.");

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();
            // != EnetityState.Deleted (ist eine EF Core interne Funktion) Die ist wichtig, weil sonst lädt die Funktion items, die in den Hauptmethoden schon als gelöscht markiert, aber noch nicht in der Db geschrieben wurden.
            // ToList() ist okay, weil das ToListAsync() oben bereits im Arbeitsspeicher hinterlegt ist, die liste muss also nicht nochmal neu aus der Db gezogen werden und es kann kein Thread blockiert werden.
            orderItems = orderItems
                .Where(oi => _context.Entry(oi).State != EntityState.Deleted)
                .ToList();

            // Rattenschwanz mit dem Rabattcode... Prüfung muss auf den unrabattierten Betrag erfolgen, wenn es darum geht die minValue für den Rabattcode gegenzufragen
            decimal netTotalWithoutDiscount = orderItems.Sum(oi => oi.LineTotal);
            
            if (netTotalWithoutDiscount < discountCode.MinOrderValue)
            {
                // Neue Hilfsfunktion, um Count herabzusetzen und den Code zu entfernen, falls einer da ist, ohne direkt eine neue Berechnung anzustoßen.
               await ClearDiscountAsync(order);

                if (recalculate) // braucht nicht zwingend: bool == true/false (in c# redundant)
                {
                    // eine Berechnung reicht, die andere gibts ja oben schon.
                    order.NetTotal = netTotalWithoutDiscount;
                    order.GrossTotal = orderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);
                }
            }
        }

        // Neue Hilfsfunktion, um nur den Zähler zu reduzieren und den Code zu entfernen, ohne aber eine Neuberechnung anzustoßen.
        private async Task ClearDiscountAsync(Order order)
        {
            if (order.DiscountCodeId != null)
            {
                var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId);
                if (discountCode != null)
                {
                    discountCode.UsedCount--;
                }
                order.DiscountCodeId = null;
            }
        }

        // Hilfsfunktion für Update/Add Orderitem, wenn nur etwas hinzugefügt wird und der MinorderValue gar nie unterschritten wird, neue Berechnung und Anwendung des Rabttes.
        private async Task RecalculateDiscountAsync(Order order)
        {
            if (order.DiscountCodeId == null) 
                return;

            var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId);

            if (discountCode == null) 
                return;

            decimal discount = discountCode.DiscountPercentage / 100m;
            order.NetTotal = order.NetTotal * (1 - discount);
            order.GrossTotal = order.GrossTotal * (1 - discount);
        }
    
    }
}



/* 
 * Vom UpdateAsync - Die DRY Idee mit der Private Function ersetzt in Update und Create diesen Codeblock:

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

    decimal netTotal = 0;
    decimal grossTotal = 0;

    foreach (var item in orderItems)
    {
        netTotal += item.LineTotal;
        grossTotal += item.LineTotal + item.TaxAmount;
    }

    decimal discount = discountCode.DiscountPercentage / 100m;
    order.NetTotal = netTotal * (1 - discount);
    order.GrossTotal = grossTotal * (1 - discount);
    discountCode.UsedCount++;
}

 */