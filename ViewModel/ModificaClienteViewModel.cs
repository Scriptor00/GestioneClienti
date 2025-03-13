namespace WebAppEF.ViewModel
{
    public class ModificaClienteViewModel
    {
        public int IdCliente { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public string Email { get; set; }
        public bool Attivo { get; set; }
        public DateTime DataIscrizione { get; set; }

        // Aggiungi un costruttore che accetta DataIscrizione
        public ModificaClienteViewModel(DateTime dataIscrizione)
        {
            DataIscrizione = dataIscrizione;
        }

        // Costruttore senza parametri per scenari in cui non si passa la DataIscrizione
        public ModificaClienteViewModel() { }
    }
}
