using System.ComponentModel.DataAnnotations;
using System;

namespace ShopBackend.Application.DTOs
{
    public class UpdateUserDto
    {
        [MaxLength(100)]
        public string? Email { get; set; }

    }
}
