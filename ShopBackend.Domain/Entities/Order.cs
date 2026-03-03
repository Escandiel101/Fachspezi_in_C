using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace ShopBackend.Domain.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Rabattcode optional (int?) , da nicht jeder Kunde einen Rabattcode haben muss
        public int? DiscountCodeId { get; set; }
        // Wenn kein Rabattcode, dann ebenso null für die Navigationsbeziehung, daher nullable (DiscountCode?).
        public DiscountCode? DiscountCode { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossTotal { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // Navigationseigenschaft für die OrderItem (Bestellpositionen), da eine Bestellung mehrere Positionen haben kann, macht eine Liste hier mehr Sinn.
        // Initialisierung mit einer leeren Liste, um Nullreferenzfehler
        public List<OrderItem> OrderItem { get; set; } = new List<OrderItem>();

        //Navpunkt zu Invoice
        public Invoice? Invoice { get; set; }

    }
}
