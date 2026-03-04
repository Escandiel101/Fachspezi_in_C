using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = "";
        [Required]
        public string NewPassword { get; set; } = "";

        [Required, Compare("NewPassword")]
        public string ConfirmPassword { get; set; } = "";
    }
}
