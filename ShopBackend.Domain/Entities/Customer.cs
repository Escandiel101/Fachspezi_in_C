using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Domain.Entities
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        // Navigationsverweis auf die User-Entität, um die Beziehung herzustellen,
        // = null! um den Compilerfehler zu vermeiden, da EF Core die Navigationseigenschaft später automatisch füllt
        public User User { get; set; } = null!;

        [Required, MaxLength(50)]
        public string FirstName { get; set; } = ""; // =""; wieder um den Compilerfehler vermeiden.

        [Required, MaxLength(50)]
        public string LastName { get; set; } = "";

        [Required, MaxLength(200)]
        public string Address { get; set; } = "";

        [Required, MaxLength(25)]
        public string Phone { get; set; } = "";

    }
}
