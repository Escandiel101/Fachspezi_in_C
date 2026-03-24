using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class LoginResponseDto
    {

        public int Id { get; set; }
        // Neu sonst kommt das Frontend nicht in meine Festung beim Login :D
        public int? CustomerId { get; set; }

        public string Role { get; set; } 

        public string Token { get; set; }
    }
}