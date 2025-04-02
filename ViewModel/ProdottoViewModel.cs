using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace GestioneClienti.ViewModel
{
    public class ProdottoViewModel
    {
        [Required(ErrorMessage = "Il nome del prodotto è obbligatorio")]
        [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri")]
        public string NomeProdotto { get; set; }

        [Required(ErrorMessage = "La categoria è obbligatoria")]
        [StringLength(50, ErrorMessage = "La categoria non può superare i 50 caratteri")]
        public string Categoria { get; set; }

        [Required(ErrorMessage = "Il prezzo è obbligatorio")]
        [Range(0.01, 10000, ErrorMessage = "Il prezzo deve essere tra 0.01 e 10,000")]
        public decimal Prezzo { get; set; }

        [Required(ErrorMessage = "La giacenza è obbligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La giacenza non può essere negativa")]
        public int Giacenza { get; set; }

        [Url(ErrorMessage = "Inserire un URL valido")]
        [StringLength(200, ErrorMessage = "L'URL non può superare i 200 caratteri")]
        public string? ImmagineUrl { get; set; }

        [Url(ErrorMessage = "Inserire un URL valido")]
        public string? TrailerUrl { get; set; }
    }
}