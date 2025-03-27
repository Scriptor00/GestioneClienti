using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
   public class CambioPasswordViewModel
{
    public string PasswordCorrente { get; set; }
    public string NuovaPassword { get; set; }
    public string ConfermaPassword { get; set; }
}
}