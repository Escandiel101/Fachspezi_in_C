using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ShopBackend.Infrastructure.Services
{
    public class CustomerService : ICustomerService
    {

        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomerService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<Customer> CreateAsync(int userId, CreateCustomerDto dto)
        {
            // Neu: Admin und Staff können keine Customer sein. Bedarf eines eigenen Accounts. 
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new KeyNotFoundException("User nicht gefunden.");
            }

            if (user.Role == UserRole.Admin || user.Role == UserRole.Staff)
            {
                throw new InvalidOperationException("Admins oder Staff können kein Kundenprofil haben.");
            }

            if (await _context.Customers
                .Where(c => c.UserId == userId)
                .AnyAsync())
            {
                // Neu nach Testen: Doppelprofil Abfangen
                throw new InvalidOperationException("Dieser User hat bereits ein Kundenprofil.");
            }
            

            var customer = new Customer
            {
                UserId = userId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Address = dto.Address,
                Phone = dto.Phone ?? ""  // Fallback, sonst meckert der Compiler mit Nullable
             };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"Kunde mit der ID: {id} nicht gefunden.");

            // Beim Überprüfen Sicherheitslücken gefunden, nicht dass sich ein Kunde einfach selbst löscht, während er noch Tausende Euro offen hat:
            var openOrders = await _context.Orders
                .Where(o => o.CustomerId == id && o.Status != "storniert")
                .AnyAsync(); //Any() gibt nur ein bool zurück!
            if (openOrders) 
                throw new ArgumentException("Kunde hat noch offene Bestellungen!");

            var openInvoices = await _context.Invoices
                .Where(i => i.Order.CustomerId == id && i.Status != "bezahlt" && i.Status != "storniert")
                .AnyAsync();
            if (openInvoices)
                throw new ArgumentException("Kunde hat noch offene Rechnungen!");

            // DSGVO Konform bräuchte es auch hier eigentlich wieder die Anonymisierung der Daten, damit, wenn man einen Kunden "löscht", nicht einfach alle zugehörigen Rechnungen weg sind. 
            // Dieses Schema ist allerdings nicht im Rahmen des Projekts enthalten. 

            _context.Customers.Remove(customer);

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Customer",
                EntityId = customer.Id,
                Action = "Delete",
                ChangedBy = changedBy,
                Details = $"KundenKonto mit der ehemaligen ID: {customer.Id} gelöscht!"
            });
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }



        public async Task<RequestCustomerDto> GetByEmailAsync(string email)
        {
            var customer = await _context.Customers
                .Where(c => c.User.Email == email)
                .Include(c => c.User)
                .FirstOrDefaultAsync();
            if (customer == null)
                throw new KeyNotFoundException($"Es existiert kein Kunde mit der gesuchten Email: {email}.");

            // YAGNI -Mark:
            // Eine kritische Stelle imho für Industriespionage. Man könnte es mit einem Rate-Limiter über program.cs und den Controler blocken
            // Oder und den Täter tracken:
            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            // normales logfile anlegen, das brauche ich leider für den Code unten.
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Customer",
                EntityId = customer.Id,
                Action = "GetByEmail",
                ChangedBy = changedBy,
                Details = $"Das Kundenkonto mit der ID: {customer.Id} wurde aufgerufen."
            });
            // Spamt mir natürlich so das Auditlog mit unwichtigen Abfragen ohne die If-Erfüllung zu. 
            // Da wäre dann vermutlich ein separates Security Log für suspicious activities sinnvoll.

            var recentLogs = await _context.AuditLogs
                .Where(a => a.ChangedBy == changedBy && a.Action == "GetByEmail" && a.ChangedAt > DateTime.UtcNow.AddMinutes(-5))
                .CountAsync();

            if (recentLogs > 10)
            {
                var alreadyWarned = await _context.AuditLogs
                    .Where(a => a.ChangedBy == changedBy
                        && a.Action == "SuspiciousActivity"
                        && a.ChangedAt > DateTime.UtcNow.AddMinutes(-5))
                    .AnyAsync(); // gibt bool true wieder, wenn alle 3 Fakten in einem LogEintrag zutreffen, ansonsten false.

                if (!alreadyWarned) // Das if not erwartet praktisch einen bool == false Eintrag von oben, um das SusLog anzulegen. Ist es aber True, gibts kein Log :)
                    _context.AuditLogs.Add(new AuditLog
                    {
                        EntityName = "Customer",
                        EntityId = customer.Id,
                        Action = "SuspiciousActivity",
                        ChangedBy = changedBy,
                        Details = $"Der User {changedBy} hat in 5 Minuten mehr als 10 Email-Abfragen gemacht!"
                    });
            }
        
            await _context.SaveChangesAsync();

            return new RequestCustomerDto
            {
                Id = customer.Id,
                UserId = customer.UserId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Address = customer.Address,
                Phone = customer.Phone ?? "",
                Email = customer.User.Email
            };

        }



        public async Task<Customer> GetByIdAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"Kunde mit der ID: {id} nicht gefunden.");
            return customer;
        }

        public async Task UpdateAsync(int id, UpdateCustomerDto dto)
        {
            var customer = await _context.Customers
                .Where(c => c.UserId == id)
                .FirstOrDefaultAsync();
            if (customer == null)
                throw new KeyNotFoundException($"Kein Kundenprofil für User-ID {id} gefunden.");

            customer.FirstName = dto.FirstName ?? customer.FirstName;
            customer.LastName = dto.LastName ?? customer.LastName;
            customer.Address = dto.Address ?? customer.Address;
            customer.Phone = dto.Phone ?? customer.Phone;

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Customer",
                EntityId = customer.Id,
                Action = "Update",
                ChangedBy = changedBy,
                Details = $"Das KundenKonto mit der ID: {customer.Id} wurde aktualisiert."
            });
            await _context.SaveChangesAsync();
        }
    }
}
