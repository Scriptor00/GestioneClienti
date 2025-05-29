using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProgettoStage.ViewModel
{
     public class CarrelloViewModel
    {
        public List<ArticoloCarrelloViewModel> Articoli { get; set; }
        public decimal TotaleCarrello { get; set; }

        public CarrelloViewModel()
        {
            Articoli = new List<ArticoloCarrelloViewModel>();
        }
    }
}