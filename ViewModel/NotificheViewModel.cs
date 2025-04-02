using System;
using System.Collections.Generic;
using System.Linq;

namespace GestioneClienti.ViewModel
{
    public class NotificheViewModel
    {
        // Informazioni utente (solo nome)
        public string NomeUtente { get; set; }
        public string RuoloUtente { get; set; } // "Admin", "Cliente", "Venditore", ecc.
        
        // Dati notifiche
        public int NumeroNotificheNonLette { get; set; }
        public List<NotificaViewModel> Notifiche { get; set; }

        // Filtri e preferenze
        public bool MostraNotifichePromozionali { get; set; }
        public bool MostraNotificheSistema { get; set; }
        public bool MostraNotificheOrdini { get; set; }
        
        // Statistiche (opzionali)
        public int TotaleNotifiche { get; set; }
        public DateTime? UltimaNotifica { get; set; }

        public NotificheViewModel()
        {
            Notifiche = new List<NotificaViewModel>();
            
            // Valori di default
            MostraNotifichePromozionali = true;
            MostraNotificheSistema = true;
            MostraNotificheOrdini = true;
        }

        // Metodo per filtrare le notifiche in base al ruolo
        public void FiltraNotifichePerRuolo()
        {
            switch (RuoloUtente)
            {
                case "Admin":
                    // Gli admin vedono tutto
                    break;
                    
                case "Venditore":
                    Notifiche = Notifiche.Where(n => n.Tipo != "Promozione" || 
                                                   n.Tipo == "OrdineVenditore").ToList();
                    break;
                    
                case "Cliente":
                    Notifiche = Notifiche.Where(n => n.Tipo != "Amministrazione").ToList();
                    break;
                    
                default:
                    // Ruolo non riconosciuto, mostra solo notifiche base
                    Notifiche = Notifiche.Where(n => n.Tipo == "Sistema" || 
                                                   n.Tipo == "Ordine").ToList();
                    break;
            }
            
            // Aggiorna i contatori
            NumeroNotificheNonLette = Notifiche.Count(n => !n.Letta);
            TotaleNotifiche = Notifiche.Count;
            UltimaNotifica = Notifiche.Any() ? Notifiche.Max(n => n.DataInvio) : (DateTime?)null;
        }
    }

    public class NotificaViewModel
    {
        // Proprietà mappate dall'entità Notifica
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Messaggio { get; set; }
        public string Tipo { get; set; }
        public DateTime DataInvio { get; set; }
        public bool Letta { get; set; }
        public string LinkAzione { get; set; }
        public int? IdRiferimento { get; set; }
        
        // Campi aggiuntivi per la view
        public string Icona { get; set; }
        public string BadgeColore { get; set; }
        public bool MostraSempre { get; set; }
        
        // Costruttore con valori di default
        public NotificaViewModel()
        {
            Icona = "fa-bell";
            BadgeColore = "bg-secondary";
            MostraSempre = false;
        }
    }
}