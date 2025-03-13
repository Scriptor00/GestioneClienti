using PuppeteerSharp;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAppEF.Controllers
{
    public class PrivacyController(Browser browser, ILogger<PrivacyController> logger) : Controller
    {
        private readonly Browser _browser = browser;
        private readonly ILogger<PrivacyController> _logger = logger;

        public async Task<IActionResult> ScaricaPDF()
        {
            _logger.LogInformation("Richiesta per generare un PDF.");

            try
            {
                var page = await _browser.NewPageAsync();
                var urlDaGenerare = Url.Action("Index", "Home", null, Request.Scheme);

                _logger.LogInformation("Caricamento della pagina per generare il PDF: {Url}", urlDaGenerare);
                await page.GoToAsync(urlDaGenerare);

                var pdfBuffer = await page.PdfDataAsync();
                _logger.LogInformation("PDF generato con successo.");

                return File(pdfBuffer, "application/pdf", "example.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la generazione del PDF.");
                return StatusCode(500, "Errore durante la generazione del PDF");
            }
        }
    }
}
