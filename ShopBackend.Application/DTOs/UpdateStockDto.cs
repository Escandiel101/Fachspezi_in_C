using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class UpdateStockDto
    {
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
    }
}
