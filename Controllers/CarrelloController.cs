using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models; 
using ProgettoStage.Entities; 
using WebAppEF.Entities;
using GestioneClienti.Entities; 
using ProgettoStage.DTOs; 
using ProgettoStage.Services;
using ProgettoStage.ViewModel; 
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System; 
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using GestioneClienti.Repositories; 
using System.Collections.Generic; 
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration; 
using System.ComponentModel.DataAnnotations;
using ProgettoStage.Repositories; 


namespace ProgettoStage.Controllers
{
    public class CarrelloController(
        ApplicationDbContext context,
        GestoreDisponibilitaProdotto availabilityManager,
        ILogger<CarrelloController> logger,
        IEmailSender emailSender, 
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment,
        GeneratorePdfService generatorePdfService
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly GestoreDisponibilitaProdotto _availabilityManager = availabilityManager;
        private readonly ILogger<CarrelloController> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender; 
        private readonly IConfiguration _configuration = configuration; 
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment; 
         private readonly GeneratorePdfService _generatorePdfService = generatorePdfService;


        // Metodo Helper per ottenere l'ID Utente come INT dalla tua tabella Utente
        private async Task<int> GetCurrentUserIdFromDb()
        {
            _logger.LogInformation("Inizio GetCurrentUserIdFromDb().");

            if (!User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Tentativo di accedere al carrello da parte di un utente non autenticato. Reindirizzamento al login o errore.");
                throw new InvalidOperationException("Utente non autenticato.");
            }

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

                    int quantitaMassimaPerUtenteNelCarrello = Math.Max(0, giacenzaReale - quantitaPrenotataDaAltri);

                    viewModel.Articoli.Add(new ArticoloCarrelloViewModel
                    {
                        IdProdotto = item.IdProdotto,
                        NomeProdotto = prodotto.NomeProdotto,
                        PrezzoUnitario = prodotto.Prezzo,
                        Quantita = item.Quantita, // Quantità attuale nel carrello dell'utente
                        DisponibilitaMagazzino = giacenzaReale, // Giacenza totale nel magazzino
                        QuantitaPrenotataDaAltri = quantitaPrenotataDaAltri, // Quantità prenotata da altri carrelli
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

    [HttpPost]
    [Route("Carrello/ConfermaOrdine")]
    [Authorize]
    public async Task<IActionResult> ConfermaOrdine([FromBody] OrdineRequestDto request)
    {
      if (!ModelState.IsValid)
      {
        return BadRequest(new
        {
            messaggio = "Dati di richiesta non validi.",
            errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
        });
    }

    if (request.ArticoliOrdine == null || !request.ArticoliOrdine.Any())
        return BadRequest(new { messaggio = "Nessun articolo per l'ordine." });

    GestioneClienti.Entities.Utente utente;
    int idUtente;

    try
    {
        string userIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                    ?? User.FindFirstValue(ClaimTypes.Name)
                                    ?? User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userIdentifier))
            return Unauthorized(new { messaggio = "Impossibile identificare l'utente autenticato." });

        utente = await _context.Utenti.AsNoTracking().FirstOrDefaultAsync(u =>
            u.Username == userIdentifier || u.Email == userIdentifier || u.Id.ToString() == userIdentifier);

        if (utente == null)
            return Unauthorized(new { messaggio = "Utente non trovato. Per favore, registrati o accedi." });

        idUtente = utente.Id;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore nel recupero dell'utente.");
        return StatusCode(500, new { messaggio = "Errore interno durante il recupero dell'utente." });
    }

    var checkResult = await _availabilityManager.PuoPiazzareOrdine(idUtente, request.ArticoliOrdine);
    if (!checkResult.successo)
        return BadRequest(new { checkResult.messaggio });

    try
    {
        var nuovoOrdine = await _availabilityManager.ProcessaOrdine(idUtente, request.ArticoliOrdine);
        if (nuovoOrdine == null)
        {
            _logger.LogError("Ordine non generato per utente {idUtente}", idUtente);
            return StatusCode(500, new { messaggio = "Errore interno nella generazione dell'ordine." });
        }

         nuovoOrdine = await _context.Ordini
                                .Include(o => o.Cliente) // Carica l'oggetto Cliente correlato
                                .FirstOrDefaultAsync(o => o.IdOrdine == nuovoOrdine.IdOrdine);

        if (nuovoOrdine == null || nuovoOrdine.Cliente == null)
        {
        _logger.LogWarning("Ordine {OrderId} o Cliente associato non trovato dopo il salvataggio.", nuovoOrdine?.IdOrdine);
        }

        // Carica i dettagli ordine con prodotti per PDF
        var dettagliOrdine = await _context.DettagliOrdini
            .Include(d => d.Prodotto)
            .Where(d => d.IdOrdine == nuovoOrdine.IdOrdine)
            .ToListAsync();

        // Aggiorna totale ordine se necessario
        if (nuovoOrdine.TotaleOrdine == 0 && dettagliOrdine.Any())
            nuovoOrdine.TotaleOrdine = dettagliOrdine.Sum(d => d.PrezzoUnitario * d.Quantita);

        //  Creazione e mappatura dell'oggetto Cliente per il PDF ---
        WebAppEF.Entities.Cliente clientePerPdf = nuovoOrdine?.Cliente; // Prendi il cliente direttamente dall'ordine caricato

        if (utente != null)
        {
            clientePerPdf = new WebAppEF.Entities.Cliente
            {
                IdCliente = utente.Id, 
                Nome = utente.Username,
                Cognome = "", 
                Email = utente.Email,
                DataIscrizione = DateTime.Now, 
                Attivo = true, 
                Indirizzo = null,
                Civico = null,
                Citta = null,
                Paese = null,
                Cap = null
            };
        }
      
        byte[] pdfBytes = null;
        try
        {
            pdfBytes = await _generatorePdfService.GeneraRicevutaOrdinePdfAsync(nuovoOrdine, dettagliOrdine, clientePerPdf);
            _logger.LogInformation("PDF per ordine {OrderId} generato con successo.", nuovoOrdine.IdOrdine);
        }
        catch (Exception exPdf)
        {
            _logger.LogError(exPdf, "Errore durante la generazione del PDF per l'ordine {OrderId}. L'email verrà inviata senza allegato.", nuovoOrdine.IdOrdine);
        }

        try
        {
            string subject = $"Conferma Ordine #{nuovoOrdine.IdOrdine} - Gaming Store";
            string destinatario = utente.Email;
            string saluto = string.IsNullOrEmpty(utente.Username) ? utente.Email : utente.Username;

            // Crea il corpo dell'email (HTML)
            string html = BuildConfirmationEmailBody(saluto, nuovoOrdine, pdfBytes); 

            if (pdfBytes != null && pdfBytes.Length > 0)
            {
                await _emailSender.SendEmailWithAttachmentAsync(
                    destinatario,
                    subject,
                    html,
                    pdfBytes,
                    $"Ricevuta_Ordine_{nuovoOrdine.IdOrdine}.pdf", // Nome del file
                    "application/pdf"                               // MIME type
                );
                _logger.LogInformation("Email con allegato PDF inviata a {Email}", destinatario);
            }
            else
            {
                await _emailSender.SendEmailAsync(destinatario, subject, html);
                _logger.LogWarning("Email inviata senza allegato PDF a {Email} per ordine {OrderId}", destinatario, nuovoOrdine.IdOrdine);
            }
        }
        catch (Exception exEmail)
        {
            _logger.LogError(exEmail, "Errore invio email per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }

        return Ok(new
        {
            messaggio = "Ordine confermato con successo! Controlla la tua email per la ricevuta.",
            ordineId = nuovoOrdine.IdOrdine,
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore critico durante la conferma ordine per utente {idUtente}", idUtente);
        return StatusCode(500, new { messaggio = "Errore interno nella conferma dell'ordine." });
    }
}

private string BuildConfirmationEmailBody(string saluto, Ordine ordine, byte[] pdfBytes)
{
    
    string body = $"<h1>Ciao {saluto},</h1>" +
                  $"<p>Il tuo ordine #{ordine.IdOrdine} è stato confermato con successo!</p>" +
                  $"<p>Totale: {ordine.TotaleOrdine:C}</p>" +
                  (pdfBytes != null && pdfBytes.Length > 0 ? "<p>Trovi la ricevuta in allegato.</p>" : "<p>Non è stato possibile allegare la ricevuta PDF. Contattaci per richiederla.</p>") +
                  "<p>Grazie per il tuo acquisto!</p>";
    return body;
}
        /// <summary>
        /// Aggiorna la quantità di un articolo nel carrello (utilizzando il GestoreDisponibilitaProdotto).
        /// </summary>
        /// <param name="request">La richiesta con l'ID del prodotto e la nuova quantità.</param>
        [HttpPost]
        [Route("Carrello/UpdateItem")]
        [ValidateAntiForgeryToken]
        [Authorize] 
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

                // Ricalcola il totale complessivo del carrello
                var updatedCartItems = await _context.Carrelli
                                                     .Where(c => c.IdUtente == idUtente)
                                                     .Include(c => c.Prodotto)
                                                     .ToListAsync();
                decimal nuovoTotaleComplessivo = updatedCartItems.Sum(c => c.Quantita * (c.Prodotto?.Prezzo ?? 0));

                return Ok(new { messaggio = "Quantità articolo carrello aggiornata con successo.", updatedItem = updatedItemViewModel, newTotal = nuovoTotaleComplessivo });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Errore di logica durante l'aggiornamento dell'articolo del carrello per utente {idUtente}, prodotto {request.ProductId}: {ex.Message}");
                return BadRequest(new { messaggio = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore critico non gestito durante l'aggiornamento dell'articolo del carrello per utente {idUtente}, prodotto {request.ProductId}.");
                return StatusCode(500, new { messaggio = "Si è verificato un errore interno durante l'aggiornamento dell'articolo del carrello." });
            }
        }
    }
}