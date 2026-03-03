using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShopBackend.Domain.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossTotal { get; set; }

        [Required, MaxLength(200)]
        public string Address { get; set; } = "";

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";

        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = "cash";

        // Das "?" hinter DateTime macht die Eigenschaft nullable, da es Fälle geben kann, in denen die Zahlung noch nicht abgeschlossen ist und somit kein Zahlungsdatum vorhanden ist.
        public DateTime? PaidAt { get; set; }
        public string Status { get; set; } = "open";
    }
}
