using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;

namespace ShopBackend.Infrastructure.Services
{
    public class CustomerService : ICustomerService
    {

        private readonly AppDbContext _context;

        public CustomerService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<Customer> CreateAsync(CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                UserId = dto.UserId,
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
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
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
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new KeyNotFoundException($"Kunde mit der ID: {id} nicht gefunden.");

            customer.FirstName = dto.FirstName ?? customer.FirstName;
            customer.LastName = dto.LastName ?? customer.LastName;
            customer.Address = dto.Address ?? customer.Address;
            customer.Phone = dto.Phone ?? customer.Phone;
            await _context.SaveChangesAsync();
        }
    }
}
