using Microsoft.AspNetCore.Mvc;
using WebAppEF.Entities;
using System.Collections.Generic;
using WebAppEF.Models;

namespace WebAppEF.Controllers
{
    public class ProdottiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProdottiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Recupera la lista dei prodotti dal database
            var prodotti = _context.Prodotti.ToList();

            // Passa la lista dei prodotti alla view
            return View(prodotti);
        }

        public IActionResult Carrello()
        {
            var prodottiNelCarrello = _context.Prodotti.ToList(); // Recupera i prodotti dal carrello
            return View(prodottiNelCarrello);
        }
    }
}