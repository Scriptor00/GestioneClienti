using Microsoft.AspNetCore.Mvc;

namespace WebAppEF.Controllers
{
    public class PrivacyController( ILogger<PrivacyController> logger) : Controller
    {
        
        private readonly ILogger<PrivacyController> _logger = logger;

        // public async Task<IActionResult> ScaricaPDF()
        // {
        //     _logger.LogInformation("Richiesta per generare un PDF.");

        //     try
        //     {
        //         var page = await _browser.NewPageAsync();
        //         var urlDaGenerare = Url.Action("Index", "Home", null, Request.Scheme);

        //         _logger.LogInformation("Caricamento della pagina per generare il PDF: {Url}", urlDaGenerare);
        //         await page.GoToAsync(urlDaGenerare);

        //         var pdfBuffer = await page.PdfDataAsync();
        //         _logger.LogInformation("PDF generato con successo.");

        //         return File(pdfBuffer, "application/pdf", "example.pdf");
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Errore durante la generazione del PDF.");
        //         return StatusCode(500, "Errore durante la generazione del PDF");
        //     }
        // }

        [HttpPost]
        public IActionResult SalvaPreferenze(string dataUsage)
        {
            
            _logger.LogInformation("Preferenze salvate: {DataUsage}", dataUsage);
            TempData["SuccessMessage"] = "Le tue preferenze sono state salvate con successo!";

            return RedirectToAction("Privacy", "Home");
        }
    }
}
