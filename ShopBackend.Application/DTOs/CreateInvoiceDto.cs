using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class CreateInvoiceDto
    {

        public int OrderId { get; set; }
        [Required, MaxLength(50)]
        public string PaymentMethod { get; set; } = "Barzahlung";

    }
}
