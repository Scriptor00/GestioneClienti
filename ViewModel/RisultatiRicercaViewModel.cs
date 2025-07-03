using WebAppEF.Entities;

namespace WebAppEF.ViewModel
{
    public class RisultatiRicercaViewModel
    {
        public List<Cliente> Clienti { get; set; } = new List<Cliente>();
        public List<Ordine> Ordini { get; set; } = new List<Ordine>();
        public string? NomeCliente { get; set; }
        public int? IdOrdine { get; set; }


    }
}