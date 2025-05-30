using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models; // Per ApplicationDbContext
using ProgettoStage.Entities; // Per Carrello, Ordine, DettagliOrdine, Prodotto
using GestioneClienti.Entities; // Per Utente
using ProgettoStage.DTOs;
using ProgettoStage.Services;
using ProgettoStage.ViewModel; // Assicurati che questo namespace esista e contenga ViewModelCarrello, ArticoloCarrelloViewModel
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System; // Per InvalidOperationException
using Microsoft.Extensions.Logging;
// Rimosso using Newtonsoft.Json; - non più necessario per la gestione del carrello via DB
using Microsoft.AspNetCore.Authorization; // Added for [Authorize] attribute

namespace ProgettoStage.Controllers
{
    // Aggiungi l'attributo [Authorize] se vuoi che solo gli utenti autenticati possano accedere al carrello
    // Uncomment this if you want to enforce authentication for all actions in this controller
    // [Authorize] 
    public class CarrelloController(ApplicationDbContext context, GestoreDisponibilitaProdotto availabilityManager, ILogger<CarrelloController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly GestoreDisponibilitaProdotto _availabilityManager = availabilityManager;
        private readonly ILogger<CarrelloController> _logger = logger;

        // Metodo Helper per ottenere l'ID Utente come INT dalla tua tabella Utente
        private async Task<int> GetCurrentUserIdFromDb()
        {
            _logger.LogInformation("Inizio GetCurrentUserIdFromDb().");

            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Tentativo di accedere al carrello da parte di un utente non autenticato. Reindirizzamento al login o errore.");
                // For API-like calls, throwing an exception is fine to be caught by the caller
                // For MVC views, you might want to redirect.
                throw new InvalidOperationException("Utente non autenticato."); 
            }

            // Questo recupera l'identificativo dall'Identity, che può essere stringa (es. email, username, GUID)
            var userIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? User.FindFirstValue(ClaimTypes.Name)
                                   ?? User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userIdentifier))
            {
                _logger.LogError("ClaimTypes.NameIdentifier, Name o Email non trovato per l'utente autenticato. Verificare le claims.");
                throw new InvalidOperationException("Identificativo utente non disponibile nelle claims. Assicurati che l'utente sia autenticato e che le claims siano impostate correttamente.");
            }
            _logger.LogDebug($"Identificativo utente recuperato dalle claims: '{userIdentifier}'. Tentativo di trovare l'ID nel DB Utenti.");

            // Qui cerchiamo l'utente nella TUA tabella 'Utenti' per ottenere il suo Id (int)
            // Assicurati che almeno una delle proprietà (Username, Email, o Id.ToString()) sia uguale a userIdentifier
            var utente = await _context.Utenti
                                       .AsNoTracking()
                                       .FirstOrDefaultAsync(u => u.Username == userIdentifier || u.Email == userIdentifier || u.Id.ToString() == userIdentifier);

            if (utente == null)
            {
                _logger.LogError($"Utente personalizzato non trovato nel database per l'identificativo: '{userIdentifier}'. Assicurati che l'utente sia registrato nella tabella 'Utenti'.");
                throw new InvalidOperationException($"Utente non trovato nel database per l'identificativo fornito. Per favore, registra o accedi.");
            }

            _logger.LogInformation($"ID Utente recuperato con successo per '{userIdentifier}': {utente.Id}. Fine GetCurrentUserIdFromDb().");
            return utente.Id; // Restituisce l'ID numerico dalla tua tabella Utenti
        }

        /// <summary>
        /// Visualizza il contenuto del carrello dell'utente corrente.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Carrello()
        {
            int idUtente;
            try
            {
                idUtente = await GetCurrentUserIdFromDb();
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Login", "Account");
            }

            var carrelloDbItems = await _context.Carrelli
                                                .Include(c => c.Prodotto)
                                                .Where(c => c.IdUtente == idUtente)
                                                .ToListAsync();

            var viewModel = new ViewModelCarrello();

            foreach (var item in carrelloDbItems)
            {
                var prodotto = item.Prodotto;

                if (prodotto != null)
                {
                    var (overallAvailableQuantity, totalBookedQuantity, giacenzaReale) =
                        await _availabilityManager.OttieniInfoDisponibilitaProdotto(item.IdProdotto);

                    int quantityInMyCart = item.Quantita;

                    int quantitaPrenotataDaAltri = Math.Max(0, totalBookedQuantity - quantityInMyCart);

                    // *** MODIFICA QUI: Assicurati che il calcolo sia per la QUANTITÀ MASSIMA TOTALE NEL CARRELLO ***
                    // Questa è la quantità massima di questo prodotto che l'utente può avere nel suo carrello.
                    // NON SOMMARE item.Quantita QUI.
                    int quantitaMassimaPerUtenteNelCarrello = Math.Max(0, giacenzaReale - quantitaPrenotataDaAltri);

                    viewModel.Articoli.Add(new ArticoloCarrelloViewModel
                    {
                        IdProdotto = item.IdProdotto,
                        NomeProdotto = prodotto.NomeProdotto,
                        PrezzoUnitario = prodotto.Prezzo,
                        Quantita = item.Quantita, // Quantità attuale nel carrello dell'utente
                        DisponibilitaMagazzino = giacenzaReale, // Giacenza totale nel magazzino
                        QuantitaPrenotataDaAltri = quantitaPrenotataDaAltri, // Quantità prenotata da altri carrelli

                        // Assegna il valore calcolato per la quantità massima consentita nel carrello
                        // Nel tuo esempio, sarà 150.
                        QuantitaOrdinabile = quantitaMassimaPerUtenteNelCarrello
                    });
                }
            }

            viewModel.TotaleComplessivo = viewModel.Articoli.Sum(a => a.PrezzoUnitario * a.Quantita);

            return View(viewModel);
        }


        /// <summary>
        /// Aggiunge o aggiorna la quantità di un prodotto nel carrello.
        /// </summary>
        /// <param name="request">DTO con IdProdotto e Quantita.</param>
        [HttpPost]
        [Route("Carrello/Aggiungi")]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure this action requires authentication
        public async Task<IActionResult> AggiungiAlCarrello([FromBody] CarrelloUpdateDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { messaggio = "Dati di richiesta non validi.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            int idUtente;
            try
            {
                // **CHIAMA IL METODO HELPER PER OTTENERE L'ID UTENTE NUMERICO**
                idUtente = await GetCurrentUserIdFromDb();
            }
            catch (InvalidOperationException ex)
            {
                // Se l'utente non è autenticato o non trova l'ID, restituisci Unauthorized
                return Unauthorized(new { messaggio = ex.Message });
            }

            try
            {
                
                await _availabilityManager.AggiungiOAggiornaArticoloCarrello(idUtente, request.IdProdotto, request.Quantita);
                return Ok(new { messaggio = "Articolo aggiunto/aggiornato al carrello con successo!" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Errore di logica durante l'aggiunta/aggiornamento al carrello per utente {idUtente}, prodotto {request.IdProdotto}: {ex.Message}");
                return BadRequest(new { messaggio = ex.Message });
            }
            catch (Exception ex)
            {
                // Logga l'errore generico
                _logger.LogError(ex, $"Errore critico non gestito durante l'aggiunta/aggiornamento al carrello per utente {idUtente}, prodotto {request.IdProdotto}.");
                return StatusCode(500, new { messaggio = "Si è verificato un errore interno durante l'aggiornamento del carrello." });
            }
        }
        

        /// <summary>
        /// Rimuove un articolo dal carrello.
        /// </summary>
        /// <param name="request">DTO con IdProdotto da rimuovere.</param>
        [HttpPost]
        [Route("Carrello/Rimuovi")]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure this action requires authentication
        public async Task<IActionResult> RimuoviDalCarrello([FromBody] CarrelloRemoveDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { messaggio = "Dati di richiesta non validi.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            int idUtente;
            try
            {
                idUtente = await GetCurrentUserIdFromDb();
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { messaggio = ex.Message });
            }

            try
            {
                await _availabilityManager.RimuoviArticoloCarrello(idUtente, request.IdProdotto);
                return Ok(new { messaggio = "Articolo rimosso dal carrello." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante la rimozione dal carrello per utente {idUtente}, prodotto {request.IdProdotto}.");
                return StatusCode(500, new { messaggio = "Si è verificato un errore interno durante la rimozione dal carrello." });
            }
        }

        /// <summary>
        /// Conferma l'ordine e sposta gli articoli dal carrello agli ordini.
        /// </summary>
        /// <param name="request">DTO con la lista degli articoli dell'ordine.</param>
        [HttpPost]
        [Route("Carrello/ConfermaOrdine")]
        [Authorize] // Ensure this action requires authentication
        public async Task<IActionResult> ConfermaOrdine([FromBody] OrdineRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { messaggio = "Dati di richiesta non validi.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }
            if (request.ArticoliOrdine == null || !request.ArticoliOrdine.Any())
            {
                return BadRequest(new { messaggio = "Nessun articolo per l'ordine." });
            }

            int idUtente;
            try
            {
                idUtente = await GetCurrentUserIdFromDb();
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { messaggio = ex.Message });
            }

            // passo l'idUtenteCorrente al metodo PuoPiazzareOrdine
            var checkResult = await _availabilityManager.PuoPiazzareOrdine(idUtente, request.ArticoliOrdine);
            if (!checkResult.successo)
            {
                return BadRequest(new { checkResult.messaggio });
            }

            try
            {
                await _availabilityManager.ProcessaOrdine(idUtente, request.ArticoliOrdine);
                // Optionally, you might want to return the new order ID or a confirmation page URL
                return Ok(new { messaggio = "Ordine confermato con successo!" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Errore di logica durante la conferma dell'ordine per utente {idUtente}: {ex.Message}");
                return BadRequest(new { messaggio = $"Errore durante la conferma dell'ordine: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Si è verificato un errore interno durante la conferma dell'ordine per utente {idUtente}.");
                return StatusCode(500, new { messaggio = $"Si è verificato un errore interno durante la conferma dell'ordine: {ex.Message}" });
            }
        }

        /// <summary>
        /// Aggiorna la quantità di un articolo nel carrello (utilizzando il GestoreDisponibilitaProdotto).
        /// </summary>
        /// <param name="request">La richiesta con l'ID del prodotto e la nuova quantità.</param>
        [HttpPost]
        [Route("Carrello/UpdateItem")]
        [ValidateAntiForgeryToken]
        [Authorize] // Ensure this action requires authentication
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { messaggio = "Dati della richiesta non validi.", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            }

            int idUtente;
            try
            {
                idUtente = await GetCurrentUserIdFromDb();
            }
            catch (InvalidOperationException ex)
            {
                return Unauthorized(new { messaggio = ex.Message });
            }

            try
            {
                await _availabilityManager.AggiungiOAggiornaArticoloCarrello(idUtente, request.ProductId, request.NewQuantity);

                var updatedCartItem = await _context.Carrelli
                    .Include(c => c.Prodotto)
                    .FirstOrDefaultAsync(c => c.IdUtente == idUtente && c.IdProdotto == request.ProductId);

                ArticoloCarrelloViewModel updatedItemViewModel = null;
                if (updatedCartItem != null && updatedCartItem.Prodotto != null)
                {
                    var (overallAvailableQuantity, totalBookedQuantity, giacenzaReale) =
                        await _availabilityManager.OttieniInfoDisponibilitaProdotto(updatedCartItem.IdProdotto);

                    int quantityInMyCart = updatedCartItem.Quantita;
                    int quantitaPrenotataDaAltri = Math.Max(0, totalBookedQuantity - quantityInMyCart);
                    int quantitaOrdinabilePersonale = Math.Max(0, giacenzaReale - quantitaPrenotataDaAltri);

                    updatedItemViewModel = new ArticoloCarrelloViewModel
                    {
                        IdProdotto = updatedCartItem.IdProdotto,
                        NomeProdotto = updatedCartItem.Prodotto.NomeProdotto,
                        PrezzoUnitario = updatedCartItem.Prodotto.Prezzo,
                        Quantita = updatedCartItem.Quantita,
                        DisponibilitaMagazzino = giacenzaReale,
                        QuantitaPrenotataDaAltri = quantitaPrenotataDaAltri,
                        QuantitaOrdinabile = quantitaOrdinabilePersonale
                    };
                }
                
                var currentCartItems = await _context.Carrelli
                                                     .Where(c => c.IdUtente == idUtente)
                                                     .ToListAsync();
                decimal newTotalPrice = 0;
                foreach(var item in currentCartItems)
                {
                    var product = await _context.Prodotti.AsNoTracking().FirstOrDefaultAsync(p => p.IdProdotto == item.IdProdotto);
                    if(product != null)
                    {
                        newTotalPrice += product.Prezzo * item.Quantita;
                    }
                }
                int newTotalItems = currentCartItems.Sum(x => x.Quantita);


                // Restituisci una risposta di successo con i dati aggiornati del carrello
                // This JSON response is suitable for an AJAX call from your front-end
                return Json(new
                {
                    success = true,
                    newTotalItems, 
                    newTotalPrice = newTotalPrice.ToString("N2"), 
                    updatedQuantity = request.NewQuantity, 
                    updatedItem = updatedItemViewModel 
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Errore di logica durante l'aggiornamento dell'articolo nel carrello per utente {idUtente}, prodotto {request.ProductId}: {ex.Message}");
                return BadRequest(new { messaggio = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore critico non gestito durante l'aggiornamento dell'articolo nel carrello per utente {idUtente}, prodotto {request.ProductId}.");
                return StatusCode(500, new { messaggio = "Si è verificato un errore interno durante l'aggiornamento del carrello." });
            }
        }
    }

    public class UpdateCartItemRequest
    {
        public int ProductId { get; set; }
        public int NewQuantity { get; set; } 
    }
}