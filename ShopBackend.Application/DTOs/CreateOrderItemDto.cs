using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class CreateOrderItemDto
    {
        // Keine OrderItemId, da diese automatisch von der Datenbank generiert wird, wenn eine neue OrderItem-Entität erstellt wird, sie existiert jetzt ja noch nicht.
        public int ProductId { get; set; } 
        public int Quantity { get; set; }
    }
}
