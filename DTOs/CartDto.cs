namespace ProgettoStage.DTOs
{
    /// <summary>
    /// DTO per aggiungere o aggiornare un articolo nel carrello.
    /// Corrisponde a CarrelloUpdateDto nel controller precedente.
    /// </summary>
    public class CarrelloUpdateDto
    {
        public int IdProdotto { get; set; }
        public int Quantita { get; set; }
    }

    /// <summary>
    /// DTO per rimuovere un articolo specifico dal carrello.
    /// Corrisponde a CarrelloRemoveDto nel controller precedente.
    /// </summary>
    public class CarrelloRemoveDto
    {
        public int IdProdotto { get; set; }
    }

    /// <summary>
    /// DTO per un singolo articolo all'interno di una richiesta d'ordine.
    /// Corrisponde a ArticoloOrdineRichiesta nel codice originale.
    /// </summary>
    public class ArticoloOrdineRichiesta
    {
        public int ProdottoId { get; set; }
        public int Quantita { get; set; }
    }

    /// <summary>
    /// DTO per un singolo articolo visualizzato nel carrello (es. per la vista).
    /// </summary>
    public class CarrelloItemDto
    {
        public int IdProdotto { get; set; }
        public int Quantita { get; set; }
        public decimal PrezzoUnitario { get; set; }
        public string? NomeProdotto { get; set; }
    }

    /// <summary>
    /// DTO per la richiesta di piazzamento dell'ordine.
    /// Corrisponde a OrdineRequestDto nel controller precedente.
    /// </summary>
    public class OrdineRequestDto
    {
        public List<ArticoloOrdineRichiesta> ArticoliOrdine { get; set; } = new List<ArticoloOrdineRichiesta>();
        // Puoi aggiungere qui altre proprietà relative all'ordine, es. indirizzo di spedizione, metodo di pagamento
    }
}