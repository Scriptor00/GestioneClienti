using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestioneClienti.Entities;
using WebAppEF.Models;

namespace GestioneClienti.Data
{
    public class NotificheFake
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            
            if (context.Notifiche.Any())
            {
                return;  
            }

            
            var notifiche = new List<Notifica>
            {
                new Notifica
                {
                    Titolo = "Offerta Esclusiva! 50% di Sconto",
                    Messaggio = "Acquista subito i nuovi giochi in offerta al 50% di sconto! Solo per oggi!",
                    DataInvio = DateTime.Now,
                    Letta = false,
                    Tipo = "Promozione",
                    LinkAzione = "https://ecommerce.com/offerta-esclusiva",
                    IdRiferimento = null  
                },
                new Notifica
                {
                    Titolo = "Il Tuo Ordine è stato Spedito",
                    Messaggio = "Il tuo ordine #12 è stato spedito e arriverà a breve!",
                    DataInvio = DateTime.Now.AddMinutes(-30),
                    Letta = false,
                    Tipo = "Ordine",
                    LinkAzione = "https://ecommerce.com/ordine/12345",
                    IdRiferimento = 12  // IdOrdine
                },
                new Notifica
                {
                    Titolo = "Evento Speciale Gaming Night",
                    Messaggio = "Unisciti alla Gaming Night di questa settimana! Partecipa e vinci fantastici premi!",
                    DataInvio = DateTime.Now.AddHours(-2),
                    Letta = true,
                    Tipo = "Evento",
                    LinkAzione = "https://ecommerce.com/eventi/gaming-night",
                    IdRiferimento = null  
                },
                new Notifica
                {
                    Titolo = "Aggiornamento disponibile per il tuo gioco preferito",
                    Messaggio = "Un nuovo aggiornamento per 'World of Gaming' è disponibile! Scopri le nuove funzionalità.",
                    DataInvio = DateTime.Now.AddDays(-1),
                    Letta = false,
                    Tipo = "Aggiornamento",
                    LinkAzione = "https://ecommerce.com/giochi/world-of-gaming",
                    IdRiferimento = null 
                },
                new Notifica
                {
                    Titolo = "Promozione Esclusiva per Membri Premium",
                    Messaggio = "Ottieni un bonus di 100 monete di gioco se acquisti un prodotto entro questa settimana.",
                    DataInvio = DateTime.Now.AddDays(-3),
                    Letta = true,
                    Tipo = "Promozione",
                    LinkAzione = "https://ecommerce.com/premiummembers/bonus",
                    IdRiferimento = null 
                }
            };

            context.Notifiche.AddRange(notifiche);
            context.SaveChanges();
        }
    }
}