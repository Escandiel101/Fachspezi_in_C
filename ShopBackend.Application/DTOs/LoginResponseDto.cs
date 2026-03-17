using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Application.DTOs
{
    public class LoginResponseDto
    {

        public int Id { get; set; }

        public string Role { get; set; } 

        public string Token { get; set; }
    }
}