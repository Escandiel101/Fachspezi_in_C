using System;
using System.ComponentModel.DataAnnotations;
using ShopBackend.Domain.Entities;

namespace ShopBackend.Application.DTOs
{
    public class CreateOrderItemDto
    {
        public int ProductId { get; set; } 
        public int Quantity { get; set; }
    }
}
