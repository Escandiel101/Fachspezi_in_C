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
