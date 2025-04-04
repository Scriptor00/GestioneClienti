using System;
using System.Collections.Generic;
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
        
        
       
    }
}