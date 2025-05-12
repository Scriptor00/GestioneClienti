using System.ComponentModel.DataAnnotations;

namespace GestioneClienti.ViewModel
{
    public class LoginViewModel
    {
        public required string Username { get; set; }
        public required string Password { get; set; }

          
        [Display(Name = "Ricordami")]
        public bool RememberMe { get; set; } 
    }
}