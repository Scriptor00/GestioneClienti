using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.Entities
{
    public class Notifica
    {
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Messaggio { get; set; }
        public DateTime DataInvio { get; set; }
        public bool Letta { get; set; }
        public string Tipo { get; set; }
        public string LinkAzione { get; set; }
        public int? IdRiferimento { get; set; }

        // Modifichiamo per usare una relazione vera
        public int UtenteId { get; set; }  // Chiave esterna
        public Utente Utente { get; set; } // Navigation property

        // Manteniamo NomeDestinatario per retrocompatibilità
        public string NomeDestinatario { get; set; }
    }

}