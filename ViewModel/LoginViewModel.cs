using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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