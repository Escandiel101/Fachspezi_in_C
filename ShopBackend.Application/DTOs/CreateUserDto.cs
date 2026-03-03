using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class CreateUserDto
    {
        [Required, MaxLength(100)]
        public string Email { get; set; } = "";
        [Required]
        public string Password { get; set; } = "";

    }
}
