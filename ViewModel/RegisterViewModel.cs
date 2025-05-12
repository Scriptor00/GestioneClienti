using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GestioneClienti.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "L'username è obbligatorio")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La password deve essere di almeno 6 caratteri")]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Conferma password è obbligatoria")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Le password non coincidono")]
        [Display(Name = "Conferma Password")]
        public string ConfermaPassword { get; set; }

        [Required(ErrorMessage = "La verifica reCAPTCHA è obbligatoria.")]
        [BindProperty(Name = "g-recaptcha-response")] 
        public string? RecaptchaResponse { get; set; }
    }
}