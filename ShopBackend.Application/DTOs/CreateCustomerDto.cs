using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class CreateCustomerDto
    {
      
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
