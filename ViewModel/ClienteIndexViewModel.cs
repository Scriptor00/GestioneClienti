using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppEF.Entities;

namespace WebAppEF.ViewModel
{
    public class ClienteIndexViewModel
    {
        public IEnumerable<Cliente> Clienti { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}