using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class UpdateCustomerDto
    {

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

