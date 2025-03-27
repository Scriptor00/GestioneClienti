using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using WebAppEF.Entities;

namespace GestioneClienti.Entities
{
    public class CarrelloItem
    {
        public int Id { get; set; }
    public string UserId { get; set; }
    public int ProdottoId { get; set; }
    public int Quantita { get; set; }
    public DateTime DataAggiunta { get; set; }
    
    [ForeignKey("ProdottoId")]
    public Prodotto Prodotto { get; set; }
    }
}