using System;
using System.ComponentModel.DataAnnotations;
using ShopBackend.Domain.Entities;


namespace ShopBackend.Application.DTOs
{
    public class UpdateOrderItemDto
    {
        public int OrderItemId { get; set; } // nicht nullable, muss immer mitgeschickt werden
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
    }
}
