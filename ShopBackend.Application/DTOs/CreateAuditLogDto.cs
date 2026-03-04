using System;
using System.ComponentModel.DataAnnotations;
using ShopBackend.Domain.Entities;

namespace ShopBackend.Application.DTOs
{
    public class CreateAuditLogDto
    {
        [Required, MaxLength(50)]
        public string EntityName { get; set; } = "";
        public int EntityId { get; set; }
        [Required, MaxLength(50)]
        public string Action { get; set; } = "";
        [MaxLength(2000)]
        public string Details { get; set; } = "";
        [Required, MaxLength(100)]
        public string ChangedBy { get; set; } = "";

    }
}