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
    public string Nome { get; set; }  // NOT NULL nel DB

    [Required]
    [StringLength(50)]
    public string Cognome { get; set; } // NOT NULL nel DB

    [Required]
    [EmailAddress]
    public string Email { get; set; } // NOT NULL nel DB

    [Required]
    public DateTime DataIscrizione { get; set; } = DateTime.Now; // NOT NULL nel DB

    [Required]
    public bool Attivo { get; set; } = true; // NOT NULL nel DB

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

    public ICollection<Ordine>? Ordini { get; set; }
}

}