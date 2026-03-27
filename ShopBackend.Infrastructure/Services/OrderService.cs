using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Diagnostics.Tracing;
using System.Numerics;
using System.Security.Claims;


namespace ShopBackend.Infrastructure.Services
{
    public class OrderService : IOrderService
    {

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppDbContext _context;

        public OrderService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
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

            await LogAction("Order", orderId, "AddOrderItem", $"Der Bestellung mit ID: {orderId} wurde eine neue Bestellposition hinzugefühgt.");
            await _context.SaveChangesAsync();

        }



        public async Task<Order> CreateAsync(CreateOrderDto dto)
        {

            // Neu - da Security Handler Policy beim POST Probleme macht und die IDs bei der Erstellung einer Bestellung eben 0 sind.. weil sie ja erst erstellt werden,
            // der Check in der Policy aber die ID schon vorher haben möchte und dann als null = NaN in JS = "null" => parseint "null" = ID mit dem Wert 0... und die gibts nicht in der DB.

            // User-Rolle und ID aus dem JWT (Token) holen
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Sicherheitsprüfung: Wenn kein Admin/Staff, muss die CustomerId zum Token passen
            if (userRole != "Admin" && userRole != "Staff")
            {
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int currentUserId))
                    throw new UnauthorizedAccessException("Ungültiges Token.");

                var customer = await _context.Customers.FindAsync(dto.CustomerId);
                if (customer == null || customer.UserId != currentUserId)
                {
                    throw new UnauthorizedAccessException("Keine Berechtigung: Du kannst keine Bestellungen für andere Kunden anlegen!");
                }
            }

            // Neu: Den Code-String in eine ID übersetzen
            int? foundDiscountId = null;
            if (!string.IsNullOrWhiteSpace(dto.DiscountCode)) // Verständlicher wäre wohl DiscountCodeCode... aber naja :D
            {
                var dc = await _context.DiscountCodes
                    .Where(dc => dc.Code == dto.DiscountCode)
                    .FirstOrDefaultAsync();
                foundDiscountId = dc?.Id;
            }


            // Neu Transaktionen. Ohne diese würde im Falle einer Exception unten bei den ganzen ifs durch das erste SaveChangesAsync(); nach dem kreieren der order 
            // praktisch eine verwaiste leere orderliste existieren, da sie hier ja bereits gespeichert wird. Save einfach weglassen?!
            // Nein! Ohne das Save gibt es in der Db keine orderId, die ich für die OrderItems aber zwingend brauche.. -> Lösung Transaktionen try/catch rollback bei exception.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = dto.CustomerId,
                    DiscountCodeId = foundDiscountId,
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

                if (foundDiscountId != null)
                {
                    await ApplyDiscountAsync(order, foundDiscountId.Value);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                // Neu: Erzwingt das Neuladen der Werte (inkl. ID) aus der DB in das Objekt ... Weil EF Core das scheinbar verliert. Frontend Test related.
                await _context.Entry(order).ReloadAsync();


                // Falls order.Id immer noch 0 sein sollte, ein Sicherheitsnetz für den Controller:
                if (order.Id == 0)
                {
                    // Suche die Bestellung manuell über den Zeitstempel/Kunde, 
                    // falls EF Core den State verloren hat
                    var fallbackOrder = await _context.Orders
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync(o => o.CustomerId == dto.CustomerId);
                    return fallbackOrder;
                }

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

            // Neue Hilfsfunktion: code entfernen, count zurücksetzen, keine neue Berechnung
            await ClearDiscountAsync(order);

            _context.Orders.Remove(order);

            // Hier nur relevant, wenn Admin oder Staff was ändern. Neu als Funktion.
            await LogAction("Order", order.Id, "Delete", $"Bestellung mit der ehemaligen ID: {order.Id} des Kunden mit der ID: {order.CustomerId} gelöscht!");
            await _context.SaveChangesAsync();
        }



        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders.ToListAsync();
        }



        public async Task<Order> GetByIdAsync(int id)
        {
            var order = await _context.Orders
            .Include(o => o.Customer)        
            .Include(o => o.OrderItems)      
            .ThenInclude(oi => oi.Product) 
            .FirstOrDefaultAsync(o => o.Id == id);
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
            // Neue Interne Security: Daten aus dem Token/URL holen.
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

            // Order laden (direkt mit Customer für den Sec-Check)
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            // Existenz-Check
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {orderId} nicht gefunden.");

            // Wieder der eigentliche Security Check:
            if (userRole != "Admin" && userRole != "Staff" && order.Customer?.UserId.ToString() != userIdString)
            {
                throw new UnauthorizedAccessException("Du hast keine Berechtigung, Artikel aus dieser Bestellung zu entfernen.");
            }
           

            // alte Logik:
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
            if (stock.ReservedQuantity < 0)
                stock.ReservedQuantity = 0;

            // Neue Umsetzung wie bei UpdateOrderItemAsync... damit es keinen Fehler mit doppeltem Rabatt beim Abzug wie vorher in den alten Berechnungen mit -= geben kann.
            _context.OrderItems.Remove(orderItem);

            var orderItems = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            var activeOrderItems = orderItems // Neuer Aufbau, da es sonst zum Fehler beim Löschen der Items im Frontend auf unter 0 Nach "Zur Kasse" kommt.
                .Where(oi => _context.Entry(oi).State != EntityState.Deleted)
                .ToList();

            order.NetTotal = activeOrderItems.Sum(oi => oi.LineTotal);
            order.GrossTotal = activeOrderItems.Sum(oi => oi.LineTotal + oi.TaxAmount);

            await RemoveDiscountIfInvalidAsync(order);

            await LogAction("Order", orderId, "RemoveOrderItem", $"OrderItem mit der ID: {orderItemId} aus Bestellung mit der ID: {orderId} entfernt.");
            await _context.SaveChangesAsync();
        }


        // Praktisch komplett verändert und massiv erweitert, um dem Frontend entsprechend zu funktionieren.
        public async Task UpdateAsync(int id, UpdateOrderDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Invoice)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) throw new KeyNotFoundException($"Bestellung {id} nicht gefunden.");

                var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

                // Neu: Status-Update und Erkennung, ob eine Bestellung aus dem Storno zurückgeholt wird
                bool wasStorniert = order.Status == "storniert";

                if (dto.Status != null)
                {
                    order.Status = dto.Status;
                }

                bool isUncancelling = wasStorniert && order.Status != "storniert";
                decimal oldGrossTotal = order.GrossTotal; // Für die spätere Rechnungsprüfung

                // Items updaten (Nur wenn der Admin welche schickt)
                // Wenn die Liste leer ist (wie beim Checkout oft der Fall), passiert hier nichts.
                if (dto.OrderItems != null && dto.OrderItems.Any())
                {
                    // Hilfsliste für zu löschende Items (Menge 0 oder im DTO nicht mehr enthalten)
                    var itemsToRemove = new List<OrderItem>();
                    foreach (var item in order.OrderItems.ToList()) // ToList() wichtig wegen Modifikation der Liste
                    {
                        var itemDto = dto.OrderItems.FirstOrDefault(oi => oi.OrderItemId == item.Id);

                        // Neue Löschen Logik: Entfernen aus Liste oder Menge <= 0
                        if (itemDto == null || (itemDto.Quantity.HasValue && itemDto.Quantity.Value <= 0))
                        {
                            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);
                            if (stock != null)
                            {
                                // immer reduzieren und absichern.
                                stock.ReservedQuantity -= item.Quantity;

                                if (stock.ReservedQuantity < 0)
                                    stock.ReservedQuantity = 0;
                            }
                            itemsToRemove.Add(item);
                        }
                        // Neue Logik für Update - Menge ändern
                        else if (itemDto.Quantity.HasValue)
                        {
                            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);
                            if (stock != null)
                            {
                                // sicherere Logik
                                int diff = itemDto.Quantity.Value - item.Quantity;
                                int newReserved = stock.ReservedQuantity + diff;

                                if (newReserved < 0)
                                    newReserved = 0;
                                stock.ReservedQuantity = newReserved;
                            }

                            item.Quantity = itemDto.Quantity.Value;
                        }
                    }

                    // Tatsächliches Löschen aus dem Kontext
                    foreach (var toRemove in itemsToRemove)
                    {
                        _context.OrderItems.Remove(toRemove);
                    }
                }

                // Neu: Wenn die Bestellung reaktiviert wird (Un-Cancel), müssen die Artikel wieder im Lager reserviert werden
                if (isUncancelling)
                {
                    var activeItemsForRestock = order.OrderItems.Where(oi => _context.Entry(oi).State != EntityState.Deleted).ToList();
                    foreach (var item in activeItemsForRestock)
                    {
                        var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);
                        if (stock != null)
                        {
                            stock.ReservedQuantity += item.Quantity;
                        }
                    }
                }

                // Rabatt-Logik alt:

                // Alten Rabatt-Zähler bereinigen
                await ClearDiscountAsync(order);

                // Basissummen neu berechnen (ohne Rabatt)
                var activeItems = order.OrderItems
                    .Where(oi => _context.Entry(oi).State != EntityState.Deleted)
                    .ToList();
                order.NetTotal = activeItems.Sum(oi => oi.LineTotal);
                order.GrossTotal = activeItems.Sum(oi => oi.LineTotal + oi.TaxAmount);

                // Rabatt anwenden, falls eine DC Id gefunden wurde
                int? foundId = null;
                if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
                {
                    var discountCode = await _context.DiscountCodes
                        .Where(dc => dc.Code == dto.DiscountCode)
                        .FirstOrDefaultAsync();
                    foundId = discountCode?.Id;
                }

                if (foundId != null)
                {
                    order.DiscountCodeId = foundId;
                    await ApplyDiscountAsync(order, foundId.Value);
                }
                else if (!string.IsNullOrWhiteSpace(dto.DiscountCode))
                {
                    throw new ArgumentException($"Der Rabattcode '{dto.DiscountCode}' ist ungültig.");
                }

                // Neue Rechnungs-Logik (Nur für Admins bei existierender Rechnung)
                // Nur neu erstellen, wenn sich der Preis ändert oder ent-storniert wird
                bool priceChanged = oldGrossTotal != order.GrossTotal;

                if (order.Invoice != null && (userRole == "Admin" || userRole == "Staff") && (priceChanged || isUncancelling))
                {
                    order.Invoice.Status = "storniert";
                    var newInvoice = new Invoice
                    {
                        OrderId = order.Id,
                        NetTotal = order.NetTotal,
                        TaxAmount = order.GrossTotal - order.NetTotal,
                        GrossTotal = order.GrossTotal,
                        FirstName = order.Invoice.FirstName,
                        LastName = order.Invoice.LastName,
                        Address = order.Invoice.Address,
                        PaymentMethod = order.Invoice.PaymentMethod,
                        Status = "offen"
                    };
                    _context.Invoices.Add(newInvoice);
                }

                // Neu: Status im Logging mit ausgeben
                await LogAction("Order", order.Id, "Update", $"Bestellung aktualisiert. Neuer Status: {order.Status}");
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
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
                if (item.Product.Stock != null) // Neuer Fix für kaputte Frontendgeschichten...
                {
                    item.Product.Stock.ReservedQuantity -= item.Quantity;

                    if (item.Product.Stock.ReservedQuantity < 0)
                        item.Product.Stock.ReservedQuantity = 0;
                }
            }


            order.Status = "storniert";
            await ClearDiscountAsync(order);

            await LogAction("Order", order.Id, "Cancel", $"Bestellung mit der ID: {order.Id} storniert");
            await _context.SaveChangesAsync();
        }


        public async Task UpdateOrderItemAsync(int orderId, int orderItemId, UpdateOrderItemDto dto)
        {
            // Neu Security Intern:
            // Daten aus der URL laden
            var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

            // Die Order laden allerdings mit einem include zum customer für die korrekte Id der Security. 
            // Das ersetzt den alten FindAsync-Aufruf komplett
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            // Existenz-Check (Muss vor dem Security-Check kommen, sonst macht order.Customer Probleme)
            if (order == null)
                throw new KeyNotFoundException($"Bestellung mit der ID: {orderId} nicht gefunden.");

            // Der eigentliche Security Check:
            if (userRole != "Admin" && userRole != "Staff" && order.Customer?.UserId.ToString() != userIdString)
            {
                throw new UnauthorizedAccessException("Du hast keine Berechtigung für diese Bestellung.");
            }

            // alte Logik:
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

            await LogAction("Order", orderId, "UpdateOrderItem", $"OrderItem mit der ID: {orderItemId} aus Bestellung mit der ID: {orderId} aktualisiert.");
            await _context.SaveChangesAsync();

        }


        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
        {
            // Neu Neu, erstmal nur ausklammern... wer weiß schon.
            // Neu Für Bestellliste anzeigen im Frontend
            //// Sicherheitsprüfung wie beim Create: Darf der User das sehen?
            //var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            //var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            //if (userRole != "Admin" && userRole != "Staff")
            
            //    var customer = await _context.Customers.FindAsync(customerId);
            //    if (customer == null || customer.UserId.ToString() != userIdString)
            //        throw new UnauthorizedAccessException("Du darfst nur deine eigenen Bestellungen sehen!");
            
                       
            return await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .ToListAsync();
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


        private async Task RemoveDiscountIfInvalidAsync(Order order)
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
                // Neu: um Count herabzusetzen und den Code zu entfernen, falls einer da ist, ohne direkt eine neue Berechnung anzustoßen.
               await ClearDiscountAsync(order);
            }
        }

        // Neu: um nur den Zähler zu reduzieren und den Code zu entfernen, ohne aber eine Neuberechnung anzustoßen.
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

        // Logging für OrderService, weil der Code eh schon so aufgebläht ist. Neu ohne Async, weil drin auch nichts Asynchron läuft. (Braucht dafür an jedem "Ausgang" händisch ein return Task)
        private Task LogAction(string entityName, int entityId, string action, string details = "")
        {
            var userRole = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Admin" && userRole != "Staff")
                return Task.CompletedTask;

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                ChangedBy = changedBy,
                Details = details
            });
            return Task.CompletedTask;
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