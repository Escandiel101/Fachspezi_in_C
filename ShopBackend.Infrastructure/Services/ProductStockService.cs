using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ShopBackend.Infrastructure.Services
{
    public class ProductStockService : IProductStockService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductStockService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }



        public async Task<Product> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description ?? "",
                Price = dto.Price,
                TaxRate = dto.TaxRate,
                IsActive = dto.IsActive,
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();


            var stock = new Stock
            {
                ProductId = product.Id,
                Quantity = 0,
                ReservedQuantity = 0
            };
            _context.Stocks.Add(stock);


            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Product",
                EntityId = product.Id,
                Action = "Create",
                ChangedBy = changedBy,
                Details = $"Produkt: {product.Name} erstellt"
            });
            await _context.SaveChangesAsync();
            return product;
        }


        public async Task<IEnumerable<Product>> GetAllActiveAsync()
        {
            return await _context.Products
                .Where(p => p.IsActive && !p.IsDeleted) // x "=>" ist wie das lambda(eigene interne Funktion) in Python z.b. filter(lambda p: p.is_active, products)
                .ToListAsync();
        }


        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Include(p => p.Stock) // erspart ca. 50 Zeilen code im Frontend Admin Panel
                .ToListAsync();
        }


        public async Task<Product> GetByIdAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {id} nicht gefunden.");
            return product;
        }


        public async Task<Stock> GetStockByProductIdAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {productId} nicht gefunden.");

            var stock = await _context.Stocks
                .Where(s => s.ProductId == productId)
                .FirstOrDefaultAsync();
            if (stock == null)
                throw new KeyNotFoundException($"Kein Lagerbestand für das Produkt mit der ID: {productId} gefunden.");

            return stock;
        }


        public async Task UpdateAsync(int id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {id} nicht gefunden.");

            product.Name = dto.Name ?? product.Name;
            product.Description = dto.Description ?? product.Description;
            product.Price = dto.Price ?? product.Price;
            product.TaxRate = dto.TaxRate ?? product.TaxRate;
            product.IsActive = dto.IsActive ?? product.IsActive;


            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Product",
                EntityId = product.Id,
                Action = "Update",
                ChangedBy = changedBy,
                Details = $"Produkt: {product.Name} aktualisiert"
            });
            await _context.SaveChangesAsync();
        }


        public async Task UpdateStockAsync(int productId, UpdateStockDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {productId} nicht gefunden.");

            var stock = await _context.Stocks
                .Where(s => s.ProductId == productId)
                .FirstOrDefaultAsync();

            if (stock == null)
                throw new KeyNotFoundException($"Kein Lagerbestand für das Produkt mit der ID: {productId} gefunden");

            if (dto.Quantity < 0)
                throw new ArgumentException("Lagerbestand darf nicht negativ sein."); 
            if (dto.ReservedQuantity < 0)
                throw new ArgumentException("Reservierter Bestand kann nicht negativ sein.");
            if (dto.ReservedQuantity > dto.Quantity)
                throw new ArgumentException("Reservierter Bestand kann nicht größer als der Gesamtbestand sein.");

            stock.ReservedQuantity = dto.ReservedQuantity;
            stock.Quantity = dto.Quantity;

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Stock",
                EntityId = stock.Id,
                Action = "Update",
                ChangedBy = changedBy,
                Details = $"Lagerbestand für Produkt: {product.Name} geändert - Gesamt: {stock.Quantity}, Reserviert: {stock.ReservedQuantity}"
            });

            await _context.SaveChangesAsync();

        }


        public async Task SoftDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {id} nicht gefunden.");

            // Status umkehren
            product.IsDeleted = !product.IsDeleted;

            // Log-Details dynamisch anpassen
            string actionDetail = product.IsDeleted ? "aus dem System entfernt" : "wieder reaktiviert";

            var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

            _context.AuditLogs.Add(new AuditLog
            {
                EntityName = "Product",
                EntityId = product.Id,
                Action = "SoftDeleteToggle", 
                ChangedBy = changedBy,
                Details = $"Produkt: {product.Name} {actionDetail}"
            });

            
            await _context.SaveChangesAsync();
        }


        public async Task HardDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {id} nicht gefunden.");

            var stock = await _context.Stocks
                .Where(s => s.ProductId == id)
                .FirstOrDefaultAsync();

            if (stock == null || stock.Quantity == 0)
            {
                _context.Products.Remove(product);

                var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
                _context.AuditLogs.Add(new AuditLog
                {
                    EntityName = "Product",
                    EntityId = product.Id,
                    Action = "HardDelete",
                    ChangedBy = changedBy,
                    Details = $"Produkt: {product.Name} gelöscht! Ehemalige Produkt ID: {product.Id}"
                });
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException($"Produkt hat noch einen Lagerbestand.");
            }

        }
    }
}
