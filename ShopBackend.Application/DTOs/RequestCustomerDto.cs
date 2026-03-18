using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class RequestCustomerDto
    {
        public int Id { get; set; } 
        public int UserId { get; set; } 
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Phone { get; set; }

        // Das DTO zeigt die Email an, auch wenn sie in der DB in einer anderen Tabelle (User) liegt.
        public string Email { get; set; } = "";
    }
}
