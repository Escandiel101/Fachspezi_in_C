using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class CreateCustomerDto
    {
        public int UserId { get; set; } // Nur für Test bzw. Lauffähigkeitszwecke, wird später durch die Authentifizierung ersetzt damit keine Angriffsfläche für die Erstellung von Kunden mit beliebigen UserIds besteht.
        [Required, MaxLength(50)]
        public string FirstName { get; set; } = "";
        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";
        [Required, MaxLength(200)]
        public string Address { get; set; } = "";
        [Required, MaxLength(25)]
        public string? Phone { get; set; } 

    }
}
