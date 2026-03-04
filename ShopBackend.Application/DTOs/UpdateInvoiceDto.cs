using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class UpdateInvoiceDto
    {
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
    }
}
