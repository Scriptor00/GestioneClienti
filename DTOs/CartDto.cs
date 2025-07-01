// Questo dovrebbe essere il contenuto del tuo file/i DTOs (es. DTOs.cs o CarrelloDtos.cs)
using System.ComponentModel.DataAnnotations; // Aggiungi questo using se non c'è

namespace ProgettoStage.DTOs
{
    /// <summary>
    /// DTO per aggiungere o aggiornare un articolo nel carrello.
    /// Corrisponde a CarrelloUpdateDto nel controller precedente.
    /// </summary>
    public class CarrelloUpdateDto
    {
        [Required]
        public int IdProdotto { get; set; }
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantità deve essere almeno 1.")]
        public int Quantita { get; set; }
    }

    /// <summary>
    /// DTO per rimuovere un articolo specifico dal carrello.
    /// Corrisponde a CarrelloRemoveDto nel controller precedente.
    /// </summary>
    public class CarrelloRemoveDto
    {
        [Required]
        public int IdProdotto { get; set; }
    }

    /// <summary>
    /// DTO per un singolo articolo all'interno di una richiesta d'ordine.
    /// Corrisponde a ArticoloOrdineRichiesta nel codice originale.
    /// </summary>
    public class ArticoloOrdineRichiesta
    {
        [Required]
        public int ProdottoId { get; set; } // <--- Corretto da IdProdotto a ProdottoId
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La quantità deve essere almeno 1.")]
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
        [Required] // È buona pratica assicurarsi che la lista non sia null
        [MinLength(1, ErrorMessage = "L'ordine deve contenere almeno un articolo.")]
        public List<ArticoloOrdineRichiesta> ArticoliOrdine { get; set; } = new List<ArticoloOrdineRichiesta>();
    }

    /// <summary>
    /// DTO per l'aggiornamento della quantità di un articolo nel carrello.
    /// Questo è il DTO che mancava e che causa l'errore CS0246.
    /// </summary>
    public class UpdateCartItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "La quantità deve essere un numero non negativo.")]
        public int NewQuantity { get; set; }
    }
}