using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Il nome è obbligatorio")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "Il cognome è obbligatorio")]
        public string Cognome { get; set; }

        [Required(ErrorMessage = "L'username è obbligatorio")]
        public string Username { get; set; }

        [Required(ErrorMessage = "L'email è obbligatoria")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "La password è obbligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Conferma la password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Le password non corrispondono")]
        public string ConfermaPassword { get; set; }
    }
}