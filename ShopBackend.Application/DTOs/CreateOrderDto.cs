using System;
using System.ComponentModel.DataAnnotations;
using ShopBackend.Domain.Entities;


namespace ShopBackend.Application.DTOs
{
    public class CreateOrderDto
    {
        
        public int CustomerId { get; set; } // Nur für Test bzw. Lauffähigkeitszwecke, wird später durch die Authentifizierung ersetzt damit keine Angriffsfläche für die Erstellung von Bestellungen mit beliebigen CustomerIds besteht.
        public int? DiscountCodeId { get; set; } 
        // Ist hier kein Navigations-Object, sondern ein Datentransfersobjekt und damit unerlässlich. 
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();

    }
}
