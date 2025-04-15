using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppEF.Entities
{
    public enum StatoOrdine
    {
        Confermato,
        Spedito,
        Annullato
    }

    public class Ordine
    {
        [Key]
        public int IdOrdine { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [ForeignKey("IdCliente")]
        public Cliente Cliente { get; set; }  // Relazione con l'entità Cliente

        public DateTime DataOrdine { get; set; } = DateTime.Now;

        [Required]
        public StatoOrdine Stato { get; set; }  // Uso dell'enum per StatoOrdine

        [Required]
        public decimal TotaleOrdine { get; set; }

      
    }
}
