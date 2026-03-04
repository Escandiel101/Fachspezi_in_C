using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface IProductStockService // Interface für die Verwaltung von Produkten und deren Lagerbeständen
    {
        Task<Product> GetByIdAsync(int id);
        Task<IEnumerable<Product>> GetAllAsync(); // Alle Produkte, unabhängig von ihrem Status (IsDeleted)
        Task<IEnumerable<Product>> GetAllActiveAsync(); // nur aktive Produkte (IsDeleted = false)
        Task<Product> CreateAsync(CreateProductDto dto);
        Task UpdateAsync (int id, UpdateProductDto dto);
        Task SoftDeleteAsync(int id);
        Task HardDeleteAsync(int id);

        Task UpdateStockAsync(int productId, int quantity, int reservedQuantity); // Methode zum Aktualisieren der Lagerbestände eines Produkts, z.B. nach einem Verkauf oder einer Rücksendung oder für interne Zwecke.
        Task<Stock> GetStockByProductIdAsync(int productId);
       

        // Stock DTOs sind damit überflüssig, da die Stock-Entität direkt in der GetStockByProductIdAsync-Methode zurückgegeben wird.
        // Das bedeutet, dass die Informationen über den Lagerbestand eines Produkts nie direkt über die Stock-Entität abgerufen oder verändert werden können. 
    }
}
