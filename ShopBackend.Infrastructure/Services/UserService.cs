using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;



namespace ShopBackend.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }



        public async Task ChangePasswordAsync(int id, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

           if (user.PasswordHash != dto.CurrentPassword) 
                throw new UnauthorizedAccessException("Falsches Passwort"); // Generische Antworten sind besser als Detailreiche. So weiß ein pot. Angreifer nicht was genau falsch ist. 

            user.PasswordHash = dto.NewPassword; // Hashing kommt später
                await _context.SaveChangesAsync();
        }


        public async Task<User> CreateAsync(CreateUserDto dto)
        {
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = dto.Password, // Hashing kommt später
                Role = "Customer",
                CreatedAt = DateTime.UtcNow,
            };

            var existingUser = await _context.Users
                .Where(u => u.Email == dto.Email)
                .AnyAsync();
            if (existingUser)
                throw new ArgumentException("Registrierung Fehlgeschlagen"); // das Frontend müsste dann hier ansetzen, das Backend gibt nur minimale Infos für potentielle Angreifer

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;


            /* TypenUNsicherer, Fehleranfälliger... länger auch so möglich: 
            await _context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Users (Email, PasswordHash, Role, CreatedAt) VALUES ({0}, {1}, {2}, {3})",
            dto.Email, dto.Password, "Customer", DateTime.UtcNow);
            */
        }


        public async Task DeleteAsync(int id)
        {

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");

            // Man braucht hier keine else oder else if, da die Methode nach dem Throwen der Exception sowieso abgebrochen wird.
            // Es ist also nicht möglich, dass der Code weiterläuft, wenn der User nicht gefunden wurde.


            var openOrders = await _context.Orders
                .Where(o => o.Customer.UserId == id && o.Status != "storniert")
                .AnyAsync();
            if (openOrders)
                throw new ArgumentException("Es sind noch Bestellungen offen!");

            var openInvoices = await _context.Invoices
                .Where(i => i.Order.Customer.UserId == id && i.Status != "storniert" && i.Status != "bezahlt")
                .AnyAsync();
            if (openInvoices)
                throw new ArgumentException("Es sind noch Rechnungen offen!");


            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
        }


        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();

        }


        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) 
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");
            return user;
        }


        public async Task UpdateAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                throw new KeyNotFoundException($"User mit der ID: {id} nicht gefunden.");
            
            user.Email = dto.Email ?? user.Email;
            // man könnte auch händischer: if (dto.Email != null) user.Email = dto.Email; machen, aber so ist es kürzer und funktioniert genauso gut
            await _context.SaveChangesAsync();
        }

    }
}
