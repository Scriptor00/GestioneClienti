using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoStage.ViewModel
{
    public class CarrelloViewModel
    {
        public List<ArticoloCarrelloViewModel> Articoli { get; set; } = new List<ArticoloCarrelloViewModel>();
        public decimal TotaleCarrello { get; set; }

        // Puoi mantenere TotaleArticoli se lo visualizzi da qualche parte, altrimenti puoi rimuoverlo
        public int TotaleArticoli => Articoli.Sum(a => a.Quantita);
    }
}