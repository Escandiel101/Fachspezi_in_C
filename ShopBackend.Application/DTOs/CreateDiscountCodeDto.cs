using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class CreateDiscountCodeDto
    {
        [Required, MaxLength(50)]
        public string Code { get; set; } = "";
        [Range(0, 50)]
        public decimal DiscountPercentage { get; set; }
        [Range(50, 1000)]
        public decimal MinOrderValue { get; set; }
        [Range(0, 1000)]
        public int MaxUses { get; set; } = 0;
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);
        public bool ConfirmLongDuration { get; set; } = false;
    }


}
