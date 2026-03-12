using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;

namespace ShopBackend.Infrastructure.Services
{
    internal class DiscountCodeService : IDiscountCodeService
    {
        private readonly AppDbContext _context;
        public DiscountCodeService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<DiscountCode> CreateAsync(CreateDiscountCodeDto dto)
        {
            var discountCode = new DiscountCode
            {
                Code = dto.Code,
                DiscountPercentage = dto.DiscountPercentage,
                MinOrderValue = dto.MinOrderValue,
                MaxUses = dto.MaxUses,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
            };

            var discountCodes = await _context.DiscountCodes
                .Where(dc => dc.Code == dto.Code)
                .FirstOrDefaultAsync();
            if (discountCodes != null)
                throw new ArgumentException($"Die Gewählte Rabattcode Bezeichnung exisistiert bereits bei {discountCodes.Id}");

            if (dto.ValidFrom < DateTime.UtcNow)
                throw new ArgumentException("Das Startdatum darf nicht in der Vergangenheit liegen");

            

           _context.DiscountCodes.Add(discountCode);
            await _context.SaveChangesAsync();
            return discountCode;

        }


        public Task<IEnumerable<DiscountCode>> GetAllAsync()
        {
            throw new NotImplementedException();
        }


        public async Task<DiscountCode> GetByIdAsync(int id)
        {
            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
                throw new KeyNotFoundException($"Rabattcode mit der ID: {id} nicht gefunden.");
            return discountCode;
        }


        public Task UpdateAsync(int id, UpdateDiscountCodeDto dto)
        {
            throw new NotImplementedException();
        }
    }
}
