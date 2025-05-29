using WebAppEF.Entities;

namespace GestioneClienti.ViewModel
{
    public class HomeViewModel
    {
        public List<Prodotto> TuttiProdotti { get; set; }
        public List<Prodotto> ProdottiPiuVenduti { get; set; }
    }
}