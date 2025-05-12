using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppEF.Entities
{
    public class DettagliOrdine
    {
        [Key]
        public int IdDettaglio { get; set; }

        [Required]
        public int IdOrdine { get; set; }

        [ForeignKey("IdOrdine")]
        public Ordine Ordine { get; set; }  

        [Required]
        public int IdProdotto { get; set; }

        [ForeignKey("IdProdotto")]
        public Prodotto Prodotto { get; set; }  

        [Range(1, int.MaxValue)]
        public int Quantita { get; set; }

        [Range(0.01, 10000)]
        public decimal PrezzoUnitario { get; set; }
    }
}