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

namespace ProgettoStage.Controllers
{
    public class CarrelloController(
        ApplicationDbContext context,
        GestoreDisponibilitaProdotto availabilityManager,
        ILogger<CarrelloController> logger,
        IEmailSender emailSender, 
        IConfiguration configuration 
        IWebHostEnvironment webHostEnvironment
        ) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly GestoreDisponibilitaProdotto _availabilityManager = availabilityManager;
        private readonly ILogger<CarrelloController> _logger = logger;
        private readonly IEmailSender _emailSender = emailSender; 
        private readonly IConfiguration _configuration = configuration; 
        private readonly _webHostEnvironment = webHostEnvironment;

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
        return BadRequest(new { messaggio = "Nessun articolo per l'ordine." });

    // Recupera utente
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

    // Controlla disponibilità prodotti
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

        // Raccoglie i dati per il PDF
        var articoliOrdinePerPdf = new List<(string NomeProdotto, int Quantita, decimal PrezzoUnitario)>();
        decimal totaleOrdine = 0;

        foreach (var articolo in request.ArticoliOrdine)
        {
            var prodotto = await _context.Prodotti.AsNoTracking().FirstOrDefaultAsync(p => p.IdProdotto == articolo.ProdottoId);
            if (prodotto != null)
            {
                articoliOrdinePerPdf.Add((prodotto.NomeProdotto, articolo.Quantita, prodotto.Prezzo));
                totaleOrdine += prodotto.Prezzo * articolo.Quantita;
            }
        }

        if (nuovoOrdine.TotaleOrdine == 0 && totaleOrdine > 0)
        {
            nuovoOrdine.TotaleOrdine = totaleOrdine;
        }
        
        byte[] pdfBytes = null;
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            byte[] logoBytes = null;
            string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logo.jpeg");

             if (System.IO.File.Exists(logoPath))
            {
            logoBytes = await System.IO.File.ReadAllBytesAsync(logoPath);
            _logger.LogInformation("Logo caricato correttamente da: {LogoPath}", logoPath);
            }
            else
            {
            _logger.LogWarning("File del logo non trovato al percorso: {LogoPath}", logoPath);
            }

            using var stream = new MemoryStream();
            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);

                    // Linea verticale sul margine sinistro
                    page.Background().Canvas((canvas, size) =>
                    {
                        float lineWidth = 6;
                        var x = lineWidth / 2;
                        canvas.DrawLine(x, 0, x, size.Height, Colors.Grey.Darken2, lineWidth);
                    });

                    // Intestazione con logo + nome azienda
                    page.Header().Element(header =>
                    {
                        header.Height(60);
                        header.Row(row =>
                        {
                            if(logoBytes != null)
                            {
                            row.ConstantItem(120)
                               .AlignLeft()
                               .Height(50)
                               .Image(logoBytes, ImageScaling.FitArea);
                            }
                            else
                            {
                                row.ConstantItem(120)
                                   .AlignLeft()
                                   .Height(50)
                                   .Text("Logo non disponibile")
                                   .Style(TextStyle.Default.FontSize(12).Italic().FontColor(Colors.Grey.Darken1));
                            }

                            row.RelativeItem()
                               .AlignCenter()
                               .AlignMiddle()
                               .Text("Gaming Store")
                               .Style(TextStyle.Default.FontSize(24).Bold().FontColor(Colors.Blue.Medium));
                        });

                        header.LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    // Corpo documento: dati ordine e cliente
                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Text("Dettagli Cliente").Style(TextStyle.Default.FontSize(14).Bold());
                        column.Item().Text($"Username: {utente.Username}");
                        column.Item().Text($"Email: {utente.Email}");

                        column.Item().Text($"Ordine #{nuovoOrdine.IdOrdine}").Style(TextStyle.Default.FontSize(14).Bold());
                        column.Item().Text($"Data: {nuovoOrdine.DataOrdine:dd/MM/yyyy HH:mm}");
                        column.Item().Text($"Stato: {nuovoOrdine.Stato}");

                        column.Item().Text("Articoli Ordinati").Style(TextStyle.Default.FontSize(14).Bold());

                        if (articoliOrdinePerPdf.Any())
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Prodotto").Bold();
                                    header.Cell().AlignRight().Text("Quantità").Bold();
                                    header.Cell().AlignRight().Text("Prezzo").Bold();
                                });

                                foreach (var a in articoliOrdinePerPdf)
                                {
                                    table.Cell().Text(a.NomeProdotto);
                                    table.Cell().AlignRight().Text(a.Quantita.ToString());
                                    table.Cell().AlignRight().Text($"{a.PrezzoUnitario:C}");
                                }
                            });
                        }
                        else
                        {
                            column.Item().Text("Nessun articolo trovato.").Italic();
                        }

                        column.Item().AlignRight().Text($"Totale: {nuovoOrdine.TotaleOrdine:C}").Bold();
                    });

                    page.Footer().AlignCenter().Column(footer =>
                    {
                        footer.Spacing(5);

                        footer.Item().Text(text =>
                        {
                            text.Span("Pagina ").FontSize(10);
                            text.CurrentPageNumber().FontSize(10);
                            text.Span(" di ").FontSize(10);
                            text.TotalPages().FontSize(10);
                        });

                        footer.Item().Text("Gaming Store S.r.l. | P.IVA 12345678901")
                                      .FontSize(10)
                                      .Italic()
                                      .FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            documento.GeneratePdf(stream);
            pdfBytes = stream.ToArray();

            _logger.LogInformation("PDF generato per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }
        catch (Exception exPdf)
        {
            _logger.LogError(exPdf, "Errore generazione PDF per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }

        // Invio email con allegato PDF
        string subject = $"Conferma Ordine #{nuovoOrdine.IdOrdine} - Gaming Store";
        string baseUrl = _configuration["BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
        string destinatario = utente.Email;
        string saluto = string.IsNullOrEmpty(utente.Username) ? utente.Email : utente.Username;

        string html = $@"
            <p>Ciao {saluto},</p>
            <p>Il tuo ordine <strong>#{nuovoOrdine.IdOrdine}</strong> è stato confermato.</p>
            <p>In allegato trovi la ricevuta in PDF.</p>
            <p><a href='{baseUrl}/Ordini/Dettagli/{nuovoOrdine.IdOrdine}'>Visualizza ordine</a></p>
            <p>Grazie per aver scelto Gaming Store!</p>";

        try
        {
            if (!string.IsNullOrEmpty(destinatario))
            {
                await _emailSender.SendEmailWithAttachmentAsync(destinatario, subject, html, pdfBytes, $"Ricevuta_Ordine_{nuovoOrdine.IdOrdine}.pdf", "application/pdf");
                _logger.LogInformation("Email inviata a {email} per ordine {id}", destinatario, nuovoOrdine.IdOrdine);
            }
        }
        catch (Exception exEmail)
        {
            _logger.LogError(exEmail, "Errore invio email per ordine {OrderId}", nuovoOrdine.IdOrdine);
        }

        return Ok(new { messaggio = "Ordine confermato! Controlla la tua email per la ricevuta." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore durante la conferma ordine per utente {idUtente}", idUtente);
        return StatusCode(500, new { messaggio = "Errore interno nella conferma dell'ordine." });
    }
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
