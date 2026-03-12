using Microsoft.EntityFrameworkCore;
using ShopBackend.Application.DTOs;
using ShopBackend.Application.Interfaces;
using ShopBackend.Domain.Entities;
using ShopBackend.Infrastructure.Data;
using System;

namespace ShopBackend.Infrastructure.Services
{
    public class DiscountCodeService : IDiscountCodeService
    {
        private readonly AppDbContext _context;
        public DiscountCodeService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<DiscountCode> CreateAsync(CreateDiscountCodeDto dto)
        {
            var discountCodes = await _context.DiscountCodes
                .Where(dc => dc.Code == dto.Code)
                .FirstOrDefaultAsync();
            if (discountCodes != null)
                throw new ArgumentException($"Die Gewählte Rabattcode Bezeichnung exisistiert bereits bei {discountCodes.Id}");

            // Man könnte ValidFrom in der Vergangenheit zulassen, wenn z.B. eine Werbung falsch umgesetzt wurde, aber das würde mein Projekt nur noch mehr aufblähen, da es wieder andere Issues erzeugen könnte. 
            if (dto.ValidFrom < DateTime.UtcNow)
                throw new ArgumentException("Das Startdatum darf nicht in der Vergangenheit liegen");

            if (dto.ValidTo < dto.ValidFrom)
                throw new ArgumentException("Das End-Datum darf nicht hinter dem Startzeitpunkt liegen");

            if (dto.ValidTo > dto.ValidFrom.AddMonths(6) && !dto.ConfirmLongDuration)
            {
                throw new ArgumentException("Der Code läuft sehr lange. Bitte bestätigen.");
            }

            var discountCode = new DiscountCode
            {
                Code = dto.Code,
                DiscountPercentage = dto.DiscountPercentage,
                MinOrderValue = dto.MinOrderValue,
                MaxUses = dto.MaxUses,
                ValidFrom = dto.ValidFrom,
                ValidTo = dto.ValidTo,
            };

            _context.DiscountCodes.Add(discountCode);
            await _context.SaveChangesAsync();

            return discountCode;
        }


        public async Task<IEnumerable<DiscountCode>> GetAllAsync()
        {
            return await _context.DiscountCodes.ToListAsync();
        }


        public async Task<DiscountCode> GetByIdAsync(int id)
        {
            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
                throw new KeyNotFoundException($"Rabattcode mit der ID: {id} nicht gefunden.");
            return discountCode;
        }


        public async Task UpdateAsync(int id, UpdateDiscountCodeDto dto)
        {
            var discountCode = await _context.DiscountCodes.FindAsync(id);
            if (discountCode == null)
                throw new KeyNotFoundException($"Rabattcode mit der ID: {id} nicht gefunden.");

            if (dto.Code != null && dto.Code != discountCode.Code)
            {
                // Wie beim Create Prüfung, ob es den Codenamen schon gibt, zusätzlich wird noch die Id mitausgespuckt, damit ein Admin leichter debuggen könnte.
                var duplicate = await _context.DiscountCodes
                    .Where(dc => dc.Code == dto.Code && dc.Id != id)
                    .FirstOrDefaultAsync();

                if (duplicate != null)
                    throw new ArgumentException($"Der Code {dto.Code} wird schon von ID {duplicate.Id} genutzt.");
            }

            discountCode.Code = dto.Code ?? discountCode.Code;
            discountCode.DiscountPercentage = dto.DiscountPercentage ?? discountCode.DiscountPercentage;
            discountCode.MinOrderValue = dto.MinOrderValue ?? discountCode.MinOrderValue;
            discountCode.MaxUses = dto.MaxUses ?? discountCode.MaxUses;

            var start = dto.ValidFrom ?? discountCode.ValidFrom;
            var end = dto.ValidTo ?? discountCode.ValidTo;

            if (end < start)
                throw new ArgumentException("Das End-Datum darf nicht hinter dem Startzeitpunkt liegen.");

            if (end > start.AddMonths(6) && !dto.ConfirmLongDuration)
                throw new ArgumentException("Der Code läuft sehr lange. Bitte bestätigen.");

            discountCode.ValidFrom = start;
            discountCode.ValidTo = end;

            await _context.SaveChangesAsync();
        }
    }
}
