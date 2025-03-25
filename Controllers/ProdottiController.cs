using Microsoft.AspNetCore.Mvc;
using WebAppEF.Entities;
using System.Collections.Generic;
using WebAppEF.Models;
using GestioneClienti.ViewModel;

namespace WebAppEF.Controllers
{
    public class ProdottiController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        public IActionResult Home()
        {
            var prodotti = _context.Prodotti.ToList();

            // Filtra solo i prodotti più venduti
            var prodottiPiuVenduti = _context.Prodotti
                .Where(p => p.NomeProdotto == "PS5" || p.NomeProdotto == "Xbox Series X")
                .ToList();

            var viewModel = new HomeViewModel
            {
                TuttiProdotti = prodotti,
                ProdottiPiuVenduti = prodottiPiuVenduti
            };

            return View(viewModel);
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

        public IActionResult Profilo()
        {
            return View();
        }
    }
}