using System.ComponentModel.DataAnnotations;

namespace GestioneClienti.ViewModel
{
    public class RichiestaRecuperoPasswordViewModel
    {
        [Required(ErrorMessage = "L'indirizzo email è obbligatorio.")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
        public string Email { get; set; }
    }
}