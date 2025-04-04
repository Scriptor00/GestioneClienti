using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class RichiestaRecuperoPasswordViewModel
    {
        [Required(ErrorMessage = "L'indirizzo email è obbligatorio.")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
        public string Email { get; set; }
    }
}