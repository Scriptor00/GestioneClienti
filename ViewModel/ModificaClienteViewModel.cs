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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ModificaClienteViewModel(DateTime dataIscrizione)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            DataIscrizione = dataIscrizione;
        }

        // Costruttore senza parametri per scenari in cui non si passa la DataIscrizione
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public ModificaClienteViewModel() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    }
}
