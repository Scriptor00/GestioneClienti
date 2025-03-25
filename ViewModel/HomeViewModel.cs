using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppEF.Entities;

namespace GestioneClienti.ViewModel
{
    public class HomeViewModel
    {
        public List<Prodotto> TuttiProdotti { get; set; }
    public List<Prodotto> ProdottiPiuVenduti { get; set; }
    }
}