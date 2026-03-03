using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Domain.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string EntityName { get; set; } = "";
       
        public int EntityId { get; set; }

        [Required, MaxLength(50)]
        public string Action { get; set; } = "";

        [MaxLength(2000)]
        public string Details { get; set; } = "";

        [Required, MaxLength(100)]
        public string ChangedBy { get; set; } = "";

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
