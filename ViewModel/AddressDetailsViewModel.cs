using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class AddressDetailsViewModel
    {
        public string Street { get; set; }             // Via/Road
        public string StreetNumber { get; set; }       // Numero civico
        public string City { get; set; }               // Città
        public string Country { get; set; }            // Paese
        public string PostalCode { get; set; }         // CAP
        public string FormattedAddress { get; set; }   // Indirizzo completo formattato
    }
}