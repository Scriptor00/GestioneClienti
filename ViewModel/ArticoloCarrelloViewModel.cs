namespace ProgettoStage.ViewModel
{
    public class ArticoloCarrelloViewModel
    {
        public int IdProdotto { get; set; }
        public string? NomeProdotto { get; set; }
        public decimal PrezzoUnitario { get; set; }
        public int Quantita { get; set; } // Quantità che l'utente ha nel carrello

        public string? ImmagineUrl { get; set; } // URL dell'immagine del prodotto

        // Proprietà per la gestione della disponibilità
        public int DisponibilitaMagazzino { get; set; } // La giacenza totale del prodotto
        public int QuantitaPrenotataDaAltri { get; set; } // Quantità in altri carrelli
        public int QuantitaOrdinabile { get; set; } // La quantità massima che questo utente può avere nel suo carrello
    }

    public class ViewModelCarrello
    {
        public List<ArticoloCarrelloViewModel> Articoli { get; set; } = new List<ArticoloCarrelloViewModel>();
        public decimal TotaleComplessivo { get; set; }

        public int TotaleArticoli => Articoli.Sum(a => a.Quantita);
    }
}