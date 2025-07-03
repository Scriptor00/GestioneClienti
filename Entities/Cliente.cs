using System.ComponentModel.DataAnnotations;

namespace WebAppEF.Entities
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome { get; set; }

        [Required]
        [StringLength(50)]
        public string Cognome { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public DateTime DataIscrizione { get; set; } = DateTime.Now;

        [Required]
        public bool Attivo { get; set; } = true;

        [StringLength(100)]
        public string? Indirizzo { get; set; }

        [StringLength(10)]
        public string? Civico { get; set; }

        [StringLength(255)]
        public string? Citta { get; set; }

        [StringLength(50)]
        public string? Paese { get; set; }

        [StringLength(255)]
        public string? Cap { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? ImportazioneErrore { get; set; }

        public ICollection<Ordine>? Ordini { get; set; }
    }
}