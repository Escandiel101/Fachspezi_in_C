using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<Customer> GetByIdAsync(int id);
        Task<IEnumerable<Customer>> GetAllAsync();
        Task<Customer> CreateAsync(CreateCustomerDto dto);
        Task UpdateAsync (int id, UpdateCustomerDto dto);
        Task DeleteAsync (int id); //Anonymisierung, nicht Löschung. DSGVO Konformität, da die Daten nicht mehr benötigt werden, aber trotzdem aufbewahrt werden müssen. Es könnte auch eine Methode geben, um Kunden zu anonymisieren, anstatt sie zu löschen, um die DSGVO-Konformität zu gewährleisten.
        // Wird aber aus Zeitgründen nicht implementiert, da es sich um eine begrenztes Projekt handelt und die Löschung hier ausreichend ist.

    }
}
