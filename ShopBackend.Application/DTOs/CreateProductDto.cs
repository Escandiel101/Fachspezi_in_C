using System;
using System.ComponentModel.DataAnnotations;



namespace ShopBackend.Application.DTOs
{
    public class CreateProductDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
        [MaxLength(1000)]
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; } = false;
        
    }
}
