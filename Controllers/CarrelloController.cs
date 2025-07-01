using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models; // Per ApplicationDbContext
using ProgettoStage.Entities; // Per Carrello, Ordine, DettagliOrdine, StatoOrdine, Prodotto
using WebAppEF.Entities; // Per ArticoloOrdineRichiesta (e se l'entità Ordine è definita qui)
using GestioneClienti.Entities; // Per Utente (se Utente e Cliente sono la stessa entità logica)
using ProgettoStage.DTOs; // Assicurati che questo namespace contenga tutti i tuoi DTO
using ProgettoStage.Services;
using ProgettoStage.ViewModel; // Assicurati che questo namespace esista e contenga ViewModelCarrello, ArticoloCarrelloViewModel
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System; // Per InvalidOperationException
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using GestioneClienti.Repositories; // Assicurati che IEmailSender sia in questo namespace o aggiungi quello corretto
using System.Collections.Generic; // Necessario per List<(string, int, decimal)>

// USING PER QUESTPDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration; // Necessario per accedere alla configurazione (es. BaseUrl)
using System.ComponentModel.DataAnnotations; // Per [Required], [Range] nei DTO


namespace ProgettoStage.Controllers
{
    // Aggiungi l'attributo [Authorize] se vuoi che solo gli utenti autenticati possano accedere al carrello
    // [Authorize]
    public class CarrelloController(
        ApplicationDbContext context,
        GestoreDisponibilitaProdotto availabilityManager,
        ILogger<CarrelloController> logger,
        IEmailSender emailSender, // Iniettiamo IEmailSender
        IConfiguration configuration // Iniettiamo IConfiguration per leggere appsettings
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly GestoreDisponibilitaProdotto _availabilityManager = availabilityManager;
        private readonly ILogger<CarrelloController> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender; // Campo per IEmailSender
        private readonly IConfiguration _configuration = configuration; // Campo per IConfiguration

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

        /// <summary>
        /// Conferma l'ordine e sposta gli articoli dal carrello agli ordini.
        /// </summary>
        /// <param name="request">DTO con la lista degli articoli dell'ordine.</param>
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
    {
        return BadRequest(new { messaggio = "Nessun articolo per l'ordine." });
    }

    int idUtente;
    GestioneClienti.Entities.Utente utente = null;

    try
    {
        var userIdentifier = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(userIdentifier))
            throw new InvalidOperationException("Identificativo utente non disponibile nelle claims.");

        utente = await _context.Utenti.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == userIdentifier || u.Email == userIdentifier || u.Id.ToString() == userIdentifier);

        if (utente == null)
        {
            _logger.LogError("Utente non trovato per identificativo: '{userIdentifier}'");
            throw new InvalidOperationException("Utente non trovato. Per favore, registrati o accedi.");
        }

        idUtente = utente.Id;
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogError(ex, "Errore durante il recupero dell'utente.");
        return Unauthorized(new { messaggio = ex.Message });
    }

    var checkResult = await _availabilityManager.PuoPiazzareOrdine(idUtente, request.ArticoliOrdine);
    if (!checkResult.successo)
    {
        return BadRequest(new { checkResult.messaggio });
    }

    try
    {
        var nuovoOrdine = await _availabilityManager.ProcessaOrdine(idUtente, request.ArticoliOrdine);
        if (nuovoOrdine == null)
        {
            _logger.LogError("Ordine non generato.");
            return StatusCode(500, new { messaggio = "Errore interno nella generazione dell'ordine." });
        }

        var articoliOrdinePerPdf = new List<(string NomeProdotto, int Quantita, decimal PrezzoUnitario)>();
        decimal totaleOrdineCalcolato = 0;

        foreach (var articolo in request.ArticoliOrdine)
        {
            var prodotto = await _context.Prodotti.AsNoTracking().FirstOrDefaultAsync(p => p.IdProdotto == articolo.ProdottoId);
            if (prodotto != null)
            {
                articoliOrdinePerPdf.Add((prodotto.NomeProdotto, articolo.Quantita, prodotto.Prezzo));
                totaleOrdineCalcolato += prodotto.Prezzo * articolo.Quantita;
            }
        }

        if (nuovoOrdine.TotaleOrdine == 0 && totaleOrdineCalcolato > 0)
        {
            nuovoOrdine.TotaleOrdine = totaleOrdineCalcolato;
        }

        byte[] pdfBytes = null;
        string pdfFileName = $"Ricevuta_Ordine_{nuovoOrdine.IdOrdine}.pdf";
        string pdfContentType = "application/pdf";

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

          using (var stream = new MemoryStream())
{
    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);

            page.Header().Row(row =>
            {
                // Contenitore logo con dimensioni forzate
                row.ConstantItem(120).Height(50).AlignLeft().Container().Padding(2).Column(logoCol =>
                {
                    if (logoBytes != null)
                    {
                        logoCol.Item()
                            .MaxWidth(116)     // Leggermente meno per il padding
                            .MaxHeight(46)     // Leggermente meno per il padding
                            .Image(logoBytes, ImageScaling.FitArea);
                    }
                    else
                    {
                        logoCol.Item()
                            .Text("Logo Mancante")
                            .FontSize(8)
                            .Italic()
                            .FontColor(Colors.Grey.Medium);
                    }
                });

                // Nome azienda centrato verticalmente
                row.RelativeItem()
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Gaming Store")
                    .Style(TextStyle.Default.FontSize(24).Bold().FontColor(Colors.Blue.Medium));
            })
            .Height(60)  // Limita l'altezza totale dell'header
            .PaddingBottom(10)
            .LineHorizontal(1)
            .LineColor(Colors.Grey.Lighten1);

            // Contenuto principale
            page.Content().Row(row =>
            {
                row.ConstantItem(10).Background(Colors.Grey.Lighten2);

                row.RelativeItem().Column(column =>
                {
                    column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                    column.Spacing(15);

                    column.Item().Text("Dettagli Cliente:").Style(TextStyle.Default.FontSize(16).Bold());
                    column.Item().Text($"Username: {utente?.Username}");
                    column.Item().Text($"Email: {utente?.Email}");

                    column.Item().PaddingTop(10).Text($"Dettagli Ordine #{nuovoOrdine.IdOrdine}:").Style(TextStyle.Default.FontSize(16).Bold());
                    column.Item().Text($"Data Ordine: {nuovoOrdine.DataOrdine:dd/MM/yyyy HH:mm}");
                    column.Item().Text($"Stato: {nuovoOrdine.Stato}");

                    column.Item().PaddingTop(10).Text("Articoli Ordinati:").Style(TextStyle.Default.FontSize(16).Bold());

                    if (articoliOrdinePerPdf.Any())
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().BorderBottom(1).PaddingBottom(5).Text("Prodotto").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Quantità").Bold();
                                header.Cell().BorderBottom(1).PaddingBottom(5).AlignRight().Text("Prezzo Unitario").Bold();
                            });

                            foreach (var item in articoliOrdinePerPdf)
                            {
                                table.Cell().Text(item.NomeProdotto);
                                table.Cell().AlignRight().Text(item.Quantita.ToString());
                                table.Cell().AlignRight().Text($"{item.PrezzoUnitario:C}");
                            }
                        });
                    }
                    else
                    {
                        column.Item().Text("Nessun articolo trovato.").Italic();
                    }

                    column.Item().PaddingTop(20).AlignRight().Text($"Totale Ordine: {nuovoOrdine.TotaleOrdine:C}").Bold();
                });
            });

            // Footer
            page.Footer().Column(footer =>
            {
                footer.Spacing(5);

                footer.Item().AlignCenter().Text(text =>
                {
                    text.Span("Pagina ").FontSize(10);
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" di ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });

                footer.Item().AlignCenter().Text("Gaming Store S.r.l. | P.IVA 12345678901")
                    .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
            });
        });
    }).GeneratePdf(stream);


            _logger.LogInformation("PDF generato per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }
        catch (Exception pdfEx)
        {
            _logger.LogError(pdfEx, "Errore generazione PDF per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }

        string emailSubject = $"Conferma Ordine #{nuovoOrdine.IdOrdine} - Gaming Store";
        var baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        var salutation = string.IsNullOrEmpty(utente?.Username) ? utente?.Email ?? "Cliente" : utente.Username;

        string emailHtml = $@"
<html>
<body>
    <p>Ciao {salutation},</p>
    <p>Il tuo ordine <strong>#{nuovoOrdine.IdOrdine}</strong> è stato confermato.</p>
    <p>In allegato trovi la ricevuta in PDF.</p>
    <p><a href='{baseUrl}/Ordini/Dettagli/{nuovoOrdine.IdOrdine}'>Visualizza ordine</a></p>
    <p>Grazie per aver scelto Gaming Store!</p>
</body>
</html>";

        try
        {
            if (!string.IsNullOrEmpty(utente?.Email))
            {
                await _emailSender.SendEmailWithAttachmentAsync(utente.Email, emailSubject, emailHtml, pdfBytes, pdfFileName, pdfContentType);
                _logger.LogInformation("Email inviata a {Email} per ordine {OrderId}", utente.Email, nuovoOrdine.IdOrdine);
            }
        }
        catch (Exception emailEx)
        {
            _logger.LogError(emailEx, "Errore invio email per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }

        return Ok(new { messaggio = "Ordine confermato con successo! Controlla la tua email per la ricevuta." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore durante la conferma ordine per utente {idUtente}");
        return StatusCode(500, new { messaggio = "Errore interno nella conferma ordine." });
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