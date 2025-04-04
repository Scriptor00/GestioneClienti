using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class RecuperoPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NuovaPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("NuovaPassword", ErrorMessage = "Le password non coincidono.")]
        public string ConfermaPassword { get; set; }

        [Required]
        public int UserId { get; set; }
    }
}