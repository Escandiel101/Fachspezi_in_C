using ShopBackend.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Application.DTOs
{
    public class UpdateUserRoleDto
    {
        
        public UserRole Role { get; set; }

    }
}