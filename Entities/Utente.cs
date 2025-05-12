using System.ComponentModel.DataAnnotations;

namespace GestioneClienti.Entities
{
    public class Utente
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

        public bool EmailConfermata { get; set; } = false;
        public string? EmailConfermaToken { get; set; }

        public int AccessFailedCount { get; set; } = 0;
        public bool LockoutEnabled { get; set; } = false;
        public DateTime? LockoutEnd { get; set; }
    }
}