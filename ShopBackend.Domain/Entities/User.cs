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
        public string Role { get; set; } = "Customer"; // ein böser Mitarbeiter bekommt Role = "Inaktiv", um den Zugriff zu sperren, ohne den Account zu löschen.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


        // Es ist sinnvoll eine Bidirektiononale Verbindung zu schaffen, damit man beim Laden eines Users direkt auch die zugehörigen Customer-Daten hat, ohne extra eine weitere Abfrage machen zu müssen.
        // Ist kritisch fürs Frontend, um Abfragechaos "von hinten her" zu vermeiden und Effizienz zu gewährleisten
        public Customer? Customer { get; set; }
                // ? ist hier notwendig, da sonst ein Admin auch zwingend einen Customer-Eintrag haben müsste, selbst wenn das nur ein Nav-Punkt ist.
    }
}
