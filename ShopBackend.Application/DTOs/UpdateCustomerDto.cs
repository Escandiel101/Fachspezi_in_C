using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class UpdateCustomerDto
    {
        public int UserId { get; set; } // Nur für Test bzw. Lauffähigkeitszwecke, wird später durch die Authentifizierung ersetzt damit keine Angriffsfläche für die Erstellung von Kunden mit beliebigen UserIds besteht.
        [MaxLength(50)]
        public string? FirstName { get; set; } 
        [MaxLength(50)]
        public string? LastName { get; set; } 
        [MaxLength(200)]
        public string?  Address { get; set; } 
        [MaxLength(50)]
        public string? Phone { get; set; }
    }
}

