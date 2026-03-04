using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface IInvoiceService
    {

        Task<Invoice> GetByIdAsync(int id);
        Task<IEnumerable<Invoice>> GetAllAsync();
        Task<Invoice> CreateAsync(CreateInvoiceDto dto);
        Task UpdateAsync (int id, UpdateInvoiceDto dto); // Löschung findet durch Stornierung statt, daher kein DeleteAsync

        Task<Invoice> GetByOrderIdAsync(int orderId); // Eine Methode, um die Rechnung anhand der Bestell-ID abzurufen

        Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId); // Eine Methode, um alle Rechnungen eines Kunden abzurufen

    }
}
