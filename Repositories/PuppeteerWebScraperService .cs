using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace WebAppEF.Repositories
{
    public class PuppeteerWebScraperService : IWebScraperService
    {
        public async Task<byte[]> GeneratePdfAsync(string htmlContent)
        {
            // Assicurati di lanciare il browser (può essere lento la prima volta)
            await new BrowserFetcher().DownloadAsync("883015");  // Usa una versione specifica

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();
            
            // Imposta il contenuto HTML della pagina
            await page.SetContentAsync(htmlContent);
            
            // Genera il PDF come byte array
            var pdfBytes = await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4 });
            
            // Chiudi il browser
            await browser.CloseAsync();

            return pdfBytes;
        }
    }
}
