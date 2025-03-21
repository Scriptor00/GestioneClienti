using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAppEF.ViewModel
{
    public class ClienteAutoCompleteViewModel
    {
        public int IdCliente { get; set; }

        public string? Nome { get; set; }
        public string? Cognome { get; set; }
    }
}