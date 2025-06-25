using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProgettoStage.Repositories;
using Microsoft.Extensions.Logging; 
using WebAppEF.Entities; 
using Microsoft.EntityFrameworkCore; 
using WebAppEF.Models;

namespace ProgettoStage.Controllers
{
    public class PdfController : Controller
    {
        private readonly GeneratorePdfService _generatorePdfService;
        private readonly ILogger<PdfController> _logger;
        private readonly ApplicationDbContext _context; 

        public PdfController(GeneratorePdfService generatorePdfService, ILogger<PdfController> logger, ApplicationDbContext context)
        {
            _generatorePdfService = generatorePdfService;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult AnteprimaPdf()
        {
            _generatorePdfService.MostraAnteprimaLayoutStandard();
            return Ok("Anteprima PDF generata con successo.");
        }

        [HttpGet("pdf/ricevuta/{idOrdine}")]
        public async Task<IActionResult> GeneraRicevutaPdf(int idOrdine)
        {
            _logger.LogInformation("Richiesta di generazione PDF per la ricevuta con ID: {IdOrdine}", idOrdine);

            if (idOrdine <= 0)
            {
                _logger.LogWarning("Richiesta non valida: ID della ricevuta non valido ({IdOrdine}).", idOrdine);
                return BadRequest("ID della ricevuta non valido.");
            }

            try
            {
                var ordine = await _context.Ordini
                                           .Include(o => o.Cliente) // Include i dati del cliente associato all'ordine
                                           .FirstOrDefaultAsync(o => o.IdOrdine == idOrdine);

                if (ordine == null)
                {
                    _logger.LogWarning("Ricevuta con ID {IdOrdine} non trovata nel database.", idOrdine);
                    return NotFound("Ricevuta non trovata.");
                }

                var dettagliOrdine = await _context.DettagliOrdini
                                                   .Where(d => d.IdOrdine == idOrdine)
                                                   .Include(d => d.Prodotto) // Include i dati del prodotto per ogni dettaglio
                                                   .ToListAsync();

                if (dettagliOrdine == null || !dettagliOrdine.Any())
                {
                    _logger.LogWarning("Nessun dettaglio ordine trovato per l'ordine con ID {IdOrdine}.", idOrdine);
                }

                // Chiamata al metodo asincrono del servizio
                byte[] pdfBytes = await _generatorePdfService.GeneraRicevutaOrdinePdfAsync(ordine, dettagliOrdine);

                _logger.LogInformation("PDF generato con successo per la ricevuta con ID: {IdOrdine}", idOrdine);
                return File(pdfBytes, "application/pdf", $"Ricevuta_Ordine_{ordine.IdOrdine}.pdf");
            }
            catch (ArgumentNullException ex) 
            {
                _logger.LogError(ex, "Errore: Dati mancanti per la generazione della ricevuta PDF per l'ordine {IdOrdine}. Messaggio: {Message}", idOrdine, ex.Message);
                return BadRequest($"Errore nella richiesta: {ex.Message}");
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Errore durante la generazione del PDF per la ricevuta con ID: {IdOrdine}", idOrdine);
                return StatusCode(500, "Errore interno del server durante la generazione del PDF.");
            }
        }

        [HttpGet("pdf/clienti")]
        public async Task<IActionResult> GeneraAnagraficaClientiPdf()
        {
            _logger.LogInformation("Richiesta di generazione PDF per l'anagrafica di tutti i clienti.");

            try
            {
                var clienti = await _context.Clienti.ToListAsync();

                if (clienti == null || !clienti.Any())
                {
                    _logger.LogWarning("Nessun cliente trovato per generare l'anagrafica PDF.");
                    return NotFound("Nessun cliente trovato per generare l'anagrafica PDF.");
                }

                // Chiamata al metodo asincrono del servizio
                byte[] pdfBytes = await _generatorePdfService.GeneraAnagraficaClientiPdfAsync(clienti);

                _logger.LogInformation("PDF anagrafica clienti generato con successo.");
                return File(pdfBytes, "application/pdf", "AnagraficaClienti.pdf");
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Errore: Dati mancanti per la generazione dell'anagrafica clienti PDF. Messaggio: {Message}", ex.Message);
                return BadRequest($"Errore nella richiesta: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la generazione del PDF per l'anagrafica clienti.");
                return StatusCode(500, "Errore interno del server durante la generazione del PDF.");
            }
        }

        [HttpGet("pdf/prodotti")]
        public async Task<IActionResult> GeneraAnagraficaProdottiPdf()
        {
            _logger.LogInformation("Richiesta di generazione PDF per l'anagrafica di tutti i prodotti.");
            try
            {
                var prodotti = await _context.Prodotti.ToListAsync();

                if (prodotti == null || !prodotti.Any())
                {
                    _logger.LogWarning("Nessun prodotto trovato per generare l'anagrafica PDF.");
                    return NotFound("Nessun prodotto trovato per generare l'anagrafica PDF.");
                }

                // Chiamata al metodo asincrono del servizio
                byte[] pdfBytes = await _generatorePdfService.GeneraAnagraficaProdottiPdfAsync(prodotti); 

                _logger.LogInformation("PDF anagrafica prodotti generato con successo.");
                return File(pdfBytes, "application/pdf", "AnagraficaProdotti.pdf"); 
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex, "Errore: Dati mancanti per la generazione dell'anagrafica prodotti PDF. Messaggio: {Message}", ex.Message);
                return BadRequest($"Errore nella richiesta: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la generazione del PDF per l'anagrafica prodotti.");
                return StatusCode(500, "Errore interno del server durante la generazione del PDF.");
            }
        }
    }
}