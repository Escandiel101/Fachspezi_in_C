using System;
using System.ComponentModel.DataAnnotations;

namespace ShopBackend.Domain.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = "";

        [Required, MaxLength(20)]
        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Wie beim Stock braucht es hier eine Nav-Beziehung zur Customer-Entität, obwohl der FK UserId in der Customer-Entität liegt, da der "Weg" sonst nur unidirektional "begehbar" sein kann!
        public Customer? Customer { get; set; }
    }
}
