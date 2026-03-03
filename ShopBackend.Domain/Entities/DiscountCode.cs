using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShopBackend.Domain.Entities
{
    public class DiscountCode
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = "";

        [Column(TypeName = "decimal(5,2)")  ]
        public  decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderValue { get; set; }

        public int MaxUses { get; set; } = 0;
        public int UsedCount { get; set; } = 0;
        public DateTime ValidFrom {  get; set; } = DateTime.UtcNow;
        public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddMonths(1);

        public bool IsExpired => DateTime.UtcNow > ValidTo || UsedCount >= MaxUses;
        public bool HasStarted => DateTime.UtcNow >= ValidFrom;
    }
}
