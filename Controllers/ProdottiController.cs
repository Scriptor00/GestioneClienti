using Microsoft.AspNetCore.Mvc;
using WebAppEF.Entities;
using System.Collections.Generic;
using WebAppEF.Models;
using GestioneClienti.ViewModel;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Spedizioni()
        {
            return View();
        }

        public IActionResult Carrello()
        {

            return View();
        }

        public IActionResult Tabella()
        {
            var prodotti = _context.Prodotti.ToList();
            return View(prodotti);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prodotto = await _context.Prodotti
                .FirstOrDefaultAsync(m => m.IdProdotto == id);
            if (prodotto == null)
            {
                return NotFound();
            }

            return View(prodotto);
        }

        // POST: Prodotti/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prodotto = await _context.Prodotti.FindAsync(id);
            if (prodotto != null)
            {
                _context.Prodotti.Remove(prodotto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Il prodotto '{prodotto.NomeProdotto}' è stato eliminato con successo.";
                return RedirectToAction(nameof(Tabella));
            }

            TempData["ErrorMessage"] = "Si è verificato un errore durante l'eliminazione del prodotto.";
            return RedirectToAction(nameof(Tabella));
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var prodotto = await _context.Prodotti.FindAsync(id);
            if (prodotto == null)
            {
                return NotFound();
            }
            return View(prodotto);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProdotto,NomeProdotto,Categoria,Prezzo,Giacenza,DataInserimento,ImmagineUrl,TrailerUrl")] Prodotto prodotto)
        {
            if (id != prodotto.IdProdotto)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(prodotto);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Il prodotto '{prodotto.NomeProdotto}' è stato modificato con successo.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Prodotti.Any(e => e.IdProdotto == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Tabella));
            }
            return View(prodotto);
        }

    }
}