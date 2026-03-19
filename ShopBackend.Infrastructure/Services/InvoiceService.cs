using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Security.Claims;

namespace ShopBackend.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor  _httpContextAccessor;

        public InvoiceService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {  
            _context = context; 
            _httpContextAccessor = httpContextAccessor;
        }

        // !!  Es fehlt eigentlich eine Zwischentabelle InvoiceItems, um die Tax-Rates verschieden besteuerter Produkte wie Kleidung 19% vs Bücher 7% etc. auf der Rechnung zu visualisieren.
        // Hier ist es nur mit TaxAmount gekürzt dargestellt. 
        // Aber wenn ich hier mit Realismus anfange, dann fehlt noch anderes (wie Response DTOs usw.) und das würde den Rahmen vollkommen sprengen - YAGNI for now.


        public async Task<Invoice> CreateAsync(CreateInvoiceDto dto)
        {
            // Transaktion wieder notwendig, da zwar keine Änderungen korrellieren, aber bei nem Datenbankfehler wäre die Rechnung geschrieben, aber der Status ggf. nicht gesetzt -> Inkonsistenz
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            { 
                // User ID aus dem http Context des JWT laden.
                var userId = int.Parse(_httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

                var order = await _context.Orders
                    .Where(o => o.Id == dto.OrderId)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync();

                if (order == null)
                    throw new KeyNotFoundException($"Keine Bestellung mit der ID: {dto.OrderId} gefunden.");

                // User Rolle aus dem Context holen.
                var userRole = _httpContextAccessor.HttpContext!.User.FindFirst(ClaimTypes.Role)?.Value;

                // Wenn der User weder Admin noch Staff ist und ihm die Order auch nicht gehört -> Fehler. False ist sozusagen der Schlüssel.
                if (userRole != "Admin" && userRole != "Staff" && order.Customer.UserId != userId)
                    throw new UnauthorizedAccessException("Keine Berechtigung.");

                // normal weiter
                if (order.Status != "ausstehend")
                    throw new ArgumentException("Nur ausstehende Bestellungen können verrechnet werden.");

                var existingInvoice = await _context.Invoices
                    .Where(i => i.OrderId == dto.OrderId)
                    .FirstOrDefaultAsync();
                if (existingInvoice != null)
                    throw new ArgumentException($"Für die Bestellung mit der der ID: {dto.OrderId} gibt es bereits eine Rechnung mit der ID: {existingInvoice.Id}.");

                var invoice = new Invoice
                {
                    OrderId = dto.OrderId,
                    PaymentMethod = dto.PaymentMethod,

                    NetTotal = order.NetTotal,
                    GrossTotal = order.GrossTotal,
                    TaxAmount = order.GrossTotal - order.NetTotal,

                    FirstName = order.Customer?.FirstName ?? "Unbekannt",
                    LastName = order.Customer?.LastName ?? "Unbekannt",
                    Address = order.Customer?.Address ?? "Keine Adresse",
                
                };

                // Geht auch mit if - if else, aber der Übung Willen -> Switch
                switch (dto.PaymentMethod)
                {
                    case "ELV": 
                        invoice.PaidAt = DateTime.UtcNow;
                        invoice.Status = "bezahlt";
                        break;

                    case "Barzahlung":
                        invoice.Status = "Zahlung per Nachnahme";
                        break;

                    default:
                        invoice.Status = "offen";
                        break;
                }
           

                _context.Invoices.Add(invoice);
                order.Status = "verarbeitet";

                // ist nicht schön, aber es braucht hier zwei Saves sonst hat das Logfile keine Rechnungs-Id zur Verfügung.
                await _context.SaveChangesAsync();

                var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _context.AuditLogs.Add(new AuditLog
                {
                    EntityName = "Invoice",
                    EntityId = invoice.Id,
                    Action = "Create",
                    ChangedBy = changedBy,
                    Details = $"Rechnung mit der ID: {invoice.Id} zur Bestellung {order.Id} erstellt."
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return invoice;
            }

            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _context.Invoices.ToListAsync();
        }


        public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId)
        {
            var invoices = await _context.Invoices
                .Where(i => i.Order.CustomerId == customerId)
                .Include(i => i.Order)
                .ToListAsync();

            if (!invoices.Any())
                throw new KeyNotFoundException($"Keine Rechnungen für den Kunden mit der ID: {customerId} gefunden.");

            return invoices;
        }


        public async Task<Invoice> GetByIdAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if(invoice == null)
                throw new KeyNotFoundException($"Keine Rechnung mit der ID: {id} gefunden.");

            return invoice;
        }


        public async Task<Invoice> GetByOrderIdAsync(int orderId)
        {
            var invoice = await _context.Invoices
                .Where(i => i.OrderId == orderId)
                .FirstOrDefaultAsync();

            if (invoice == null)
                throw new KeyNotFoundException($"Keine Rechnung zur Bestell-ID: {orderId} gefunden.");

            return invoice;
        }


        public async Task UpdateAsync(int id, UpdateInvoiceDto dto)
        {

            
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
                throw new KeyNotFoundException($"Keine Rechnung mit der ID: {id} gefunden.");

            if (invoice.Status == "storniert")
                throw new ArgumentException("Eine stornierte Rechnung kann nicht mehr bearbeitet werden.");

            invoice.PaymentMethod = dto.PaymentMethod ?? invoice.PaymentMethod;
            invoice.Status = dto.Status ?? invoice.Status;

            if (dto.PaidAt.HasValue)
            {
                invoice.PaidAt = dto.PaidAt.Value;
            }

            if (invoice.Status == "bezahlt" && invoice.PaidAt == null)
            {
                invoice.PaidAt = DateTime.UtcNow;
            }

            else if (invoice.Status == "offen" || invoice.Status == "Zahlung per Nachnahme")
            {
                invoice.PaidAt = null; 
            }

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Invoice",
                EntityId = invoice.Id,
                Action = "Update",
                ChangedBy = changedBy,
                Details = $"Rechnung mit der ID: {invoice.Id} aktualisiert - Status: {invoice.Status}, Zahlungsmethode: {invoice.PaymentMethod}."
            });

            await _context.SaveChangesAsync();
        }
    }
}
