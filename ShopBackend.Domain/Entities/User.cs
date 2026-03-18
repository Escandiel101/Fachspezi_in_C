using System;
using System.ComponentModel.DataAnnotations;


namespace ShopBackend.Domain.Entities
{
    // Update zur Nutzerrolle für den Authorization Handler, um pot. Fehler mit string zu vermeiden. Id Ranges für Erweiterungen eingeplant.
    public enum UserRole
    {
        Admin = 0,
        Staff = 20,
        Customer = 50,
        Inactive = 99
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = "";
        
        public UserRole Role { get; set; }  

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        // Es ist sinnvoll eine Bidirektiononale Verbindung zu schaffen, damit man beim Laden eines Users direkt auch die zugehörigen Customer-Daten hat, ohne extra eine weitere Abfrage machen zu müssen.
        // Ist kritisch fürs Frontend, um Abfragechaos "von hinten her" zu vermeiden und Effizienz zu gewährleisten
        public Customer? Customer { get; set; }
                // ? ist hier notwendig, da sonst ein Admin auch zwingend einen Customer-Eintrag haben müsste, selbst wenn das nur ein Nav-Punkt ist.
    }
}
