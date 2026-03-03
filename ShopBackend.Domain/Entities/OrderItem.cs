using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShopBackend.Domain.Entities
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; } 

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        public decimal LineQuantity => Quantity * UnitPrice;
        public decimal TaxAmount => LineQuantity * (TaxRate / 100);


    }
}
