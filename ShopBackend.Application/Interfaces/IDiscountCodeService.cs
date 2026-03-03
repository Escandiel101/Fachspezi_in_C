using System;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

namespace ShopBackend.Application.Interfaces
{
    public interface IDiscountCodeService
    {

        Task<DiscountCode> GetByIdAsync(int id);
        Task<IEnumerable<DiscountCode>> GetAllAsync(); 
        Task<DiscountCode> CreateAsync(CreateDiscountCodeDto dto);
        Task UpdateAsync (int id, UpdateDiscountCodeDto dto);

    }
}
