using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

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

        public DateTime DataIscrizione { get; set; } = DateTime.Now;

        public bool Attivo { get; set; } = true;

        public ICollection<Ordine>? Ordini { get; set; }

    }
}