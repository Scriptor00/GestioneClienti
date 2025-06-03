using System.Diagnostics;
using GestioneClienti.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;
using WebAppEF.Models;

namespace WebAppEF.Controllers
{
    public class HomeController(ILogger<HomeController> logger, ApplicationDbContext context) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly ApplicationDbContext _context = context;

        [Authorize(Roles = "Admin")]
        public IActionResult Dashboard()
        {

            return View();
        }

        // Azione per la home page
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            ViewData["IsAuthenticated"] = User.Identity.IsAuthenticated;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            _logger.LogInformation("Accesso alla home page.");

            // Recupero dati per la dashboard
            ViewBag.OrdiniTotali = await _context.Ordini.CountAsync();
            ViewBag.ClientiTotali = await _context.Clienti.CountAsync();

            ViewBag.ClientiAttivi = await _context.Clienti.CountAsync(c => c.Attivo);
            ViewBag.ClientiInattivi = await _context.Clienti.CountAsync(c => !c.Attivo);

            ViewBag.OrdiniConfermati = await _context.Ordini.CountAsync(o => o.Stato == StatoOrdine.Confermato);
            ViewBag.OrdiniSpediti = await _context.Ordini.CountAsync(o => o.Stato == StatoOrdine.Spedito);
            ViewBag.OrdiniAnnullati = await _context.Ordini.CountAsync(o => o.Stato == StatoOrdine.Annullato);

            var ordiniMensili = _context.Ordini
       .GroupBy(o => o.DataOrdine.Month)
       .Select(g => new { Month = g.Key, Count = g.Count() })
       .OrderBy(o => o.Month)
       .ToList();


            ViewBag.OrdiniMensili = ordiniMensili.Select(o => o.Count).ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            _logger.LogInformation("Accesso alla pagina Privacy Policy.");
            ViewData["Title"] = "Privacy Policy";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            _logger.LogError("Errore riscontrato. RequestId: {RequestId}", requestId);

            return View(new ErrorViewModel { RequestId = requestId });
        }

        public IActionResult Contatti()
        {

            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AggiungiProdotto()
        {
            return View(new ProdottoViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AggiungiProdotto(Prodotto prodotto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    prodotto.DataInserimento = DateTime.Now;

                    _context.Prodotti.Add(prodotto);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Prodotti"); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante l'aggiunta del prodotto");
                    ModelState.AddModelError("", "Si è verificato un errore durante il salvataggio.");
                }
            }

            return View(prodotto);
        }

        public IActionResult Chat()
{
    return View();
}

    }
}
