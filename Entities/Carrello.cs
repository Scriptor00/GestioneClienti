using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestioneClienti.Entities; // Per la tua entità Utente
using WebAppEF.Entities; // Per Prodotto, Ordine, DettagliOrdine, ecc. (se sono qui)

namespace ProgettoStage.Entities // Assicurati che l'entità Carrello sia in un namespace appropriato, es. ProgettoStage.Entities
{
    [Table("Carrelli")] // Assicurati che il nome della tabella nel DB sia "Carrelli"
    public class Carrello
    {
        [Key]
        public int IdCarrello { get; set; } 

        [Required]
        // IdUtente è la chiave esterna che punta a Utente.Id
        // Abbiamo cambiato il nome della proprietà per chiarezza, da IdCliente a IdUtente
        public int IdUtente { get; set; } 

        [Required]
        public int IdProdotto { get; set; }

        [Required]
        public int Quantita { get; set; }

        [Required]
        public DateTime DataUltimoAggiornamento { get; set; }

        // Navigation Property: Ora si riferisce alla tua entità Utente
        // L'attributo ForeignKey deve puntare alla proprietà che è la chiave esterna (IdUtente in questo caso)
        // e non direttamente alla PK della tabella referenziata (Id in Utente).
        [ForeignKey("IdUtente")] // Questa chiave esterna punta a Utente.Id
        public virtual Utente Utente { get; set; } 

        // Navigation Property per il Prodotto
        [ForeignKey("IdProdotto")]
        public virtual Prodotto Prodotto { get; set; }
    }
}