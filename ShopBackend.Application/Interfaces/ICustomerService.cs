using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<Customer> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync(); 
        Task<Customer> CreateAsync(int userId, CreateCustomerDto dto);
        Task UpdateAsync (int id, UpdateCustomerDto dto);
        Task DeleteAsync (int id); // Anonymisierung, nicht Löschung. DSGVO Konformität, da die Daten nicht mehr benötigt werden, aber trotzdem aufbewahrt werden müssen.
        // Es könnte auch eine Methode geben, um Kunden zu anonymisieren, anstatt sie zu löschen, um die DSGVO-Konformität zu gewährleisten.
        // Basis in AppDbContext.cs vorhanden, die vollständige Logik wird jedoch nicht implementiert, da man irgendwo Aufhören muss in einem kleinen Projekt.

        // Neue Methode, damit der Kundenservice auch Anrufern helfen kann.
        Task<RequestCustomerDto> GetByEmailAsync(string email);
    }
}
