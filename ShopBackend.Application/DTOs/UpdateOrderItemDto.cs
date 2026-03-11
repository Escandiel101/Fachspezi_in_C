using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class UpdateOrderItemDto
    {
        public int OrderItemId { get; set; } // nicht nullable, muss immer mitgeschickt werden, notwendig beim Update, damit die OrderItem-Entität in der Datenbank gefunden und aktualisiert werden kann.
        public int? Quantity { get; set; }
    }
}
