using System;
using System.ComponentModel.DataAnnotations;
using ShopBackend.Domain.Entities;


namespace ShopBackend.Application.DTOs
{
    public class UpdateOrderDto
    {
        public string? DiscountCode { get; set; }
        public string? Status { get; set; } // Neu Für Frontend Funktionalität
        // Ist hier kein Navigations-Object, sondern ein Datentransfersobjekt und damit unerlässlich, um Zugriff auf die OrderItems zu haben, um sie zu aktualisieren. (Menge und Produkt selbst) 
        public List<UpdateOrderItemDto>? OrderItems { get; set; } = new List<UpdateOrderItemDto>();
    }
}
