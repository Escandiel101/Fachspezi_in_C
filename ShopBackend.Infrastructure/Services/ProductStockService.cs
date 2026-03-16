using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;
using System.Collections;

namespace ShopBackend.Infrastructure.Services
{
    public class ProductStockService : IProductStockService
    {
        private readonly AppDbContext _context;

        public ProductStockService(AppDbContext context)
        {
            _context = context;
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
            return await _context.Products.ToListAsync();
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

            await _context.SaveChangesAsync();

        }


        public async Task SoftDeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                throw new KeyNotFoundException($"Produkt mit der ID: {id} nicht gefunden.");

            product.IsDeleted = true;
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
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new ArgumentException($"Produkt hat noch einen Lagerbestand.");
            }

        }
    }
}
