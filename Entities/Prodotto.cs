using System.ComponentModel.DataAnnotations;

namespace WebAppEF.Entities
{
    public class Prodotto
    {
        [Key]
        public int IdProdotto { get; set; }

        [Required]
        [StringLength(100)]
        public string NomeProdotto { get; set; }

        [Required]
        [StringLength(50)]
        public string Categoria { get; set; }

        [Range(0, 10000)]
        public decimal Prezzo { get; set; }

        public int Giacenza { get; set; }

        public DateTime DataInserimento { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? ImmagineUrl { get; set; }

        public string? TrailerUrl { get; set; }
        
        [Timestamp] // AGGIUNGI QUESTA LINEA
        public byte[] RowVersion { get; set; }
    }
}