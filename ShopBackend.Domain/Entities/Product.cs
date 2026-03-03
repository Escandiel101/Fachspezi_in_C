using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShopBackend.Domain.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(1000)]
        public string Description { get; set; } = "";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxRate { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        // Braucht eine Nav-Beziehung zum Stock, obwohl der FK ProductId in der Stock-Entität liegt. Erklärung im User.cs
        public Stock? Stock { get; set; }
    }
}
