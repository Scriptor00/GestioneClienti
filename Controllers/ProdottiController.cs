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

        public IActionResult Index(int page = 1, string search = "", string category = "", string sort = "")
        {
            int pageSize = 8;
            var query = _context.Prodotti.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.NomeProdotto.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Categoria == category);
            }

            switch (sort)
            {
                case "asc":
                    query = query.OrderBy(p => p.Prezzo);
                    break;
                case "desc":
                    query = query.OrderByDescending(p => p.Prezzo);
                    break;
                default:
                    query = query.OrderBy(p => p.NomeProdotto);
                    break;
            }

            var totalItems = query.Count();
            var prodotti = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.Sort = sort;

            return View(prodotti);
        }
        public IActionResult Profilo()
        {

            var viewModel = new ProfiloViewModel
            {
                Username = User.Identity.Name,
                Email = "carlodicuonzo@yahoo.com",
                DataRegistrazione = new DateTime(2022, 1, 15),
                Livello = 50,
                IsPremium = true,
                EsperienzaPercentuale = 75,
                Punteggio = 12450,
                GiochiPosseduti = 47,
                OreGiocate = 328,
                AchievementsSbloccati = 24,
                SfideCompletate = 8
            };

            return View(viewModel);
        }

        public IActionResult ModificaProfilo()
        {
            return View();
        }


        public IActionResult Sicurezza()
        {
            // Passa la password (in realtà dovresti recuperarla dal database)
            ViewBag.Password = "laPasswordCorrente";
            return View();
        }

        public IActionResult Notifiche()
        {

            var notifiche = new List<NotificaViewModel>
    {
        new NotificaViewModel
        {
            Id = 1,
            Titolo = "Offerta Speciale",
            Messaggio = "Non perdere la nostra offerta esclusiva del 50% su tutti i prodotti!",
            Tipo = "Promozione",
            DataInvio = DateTime.Now.AddHours(-2),
            Letta = false,
            LinkAzione = "http://example.com/offerta",
            IdRiferimento = null
        },
        new NotificaViewModel
        {
            Id = 2,
            Titolo = "Ordine Spedito",
            Messaggio = "Il tuo ordine #1234 è stato spedito.",
            Tipo = "Ordine",
            DataInvio = DateTime.Now.AddHours(-1),
            Letta = true,
            LinkAzione = null,
            IdRiferimento = 1234
        },
        new NotificaViewModel
        {
            Id = 3,
            Titolo = "Notifica Importante",
            Messaggio = "La tua sessione di gioco è stata interrotta per manutenzione.",
            Tipo = "Importante",
            DataInvio = DateTime.Now.AddMinutes(-30),
            Letta = false,
            LinkAzione = null,
            IdRiferimento = null
        }
    };

            // Creiamo il ViewModel da passare alla vista
            var notificheViewModel = new NotificheViewModel
            {
                NumeroNotificheNonLette = notifiche.Count(n => !n.Letta), // Conta le notifiche non lette
                Notifiche = notifiche
            };

            return View(notificheViewModel);
        }

        public IActionResult Spedizioni()
        {
            return View();
        }

        public IActionResult Carrello()
        {

            return View();
        }

    }
}