using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.Repositories;
using WebAppEF.ViewModel;
using WebAppEF.ViewModels;

namespace WebAppEF.Controllers
{
    public class OrdiniController(ApplicationDbContext context, IOrdiniRepository ordiniRepository, ICustomerRepository customerRepository, ILogger<OrdiniController> logger) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IOrdiniRepository _ordiniRepository = ordiniRepository;
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly ILogger<OrdiniController> _logger = logger;

        // lista ordini
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                // Calcola il numero totale di ordini
                int totalItems = await _ordiniRepository.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Recupera gli ordini paginati di 10 in 10
                var ordini = await _ordiniRepository.GetAllPaged(page, pageSize);

                // nuovo costruttore
                var ordiniViewModel = ordini.Select(o => new OrdineViewModel
                {
                    IdOrdine = o.IdOrdine,
                    Cliente = new ClienteViewModel
                    {
                        IdCliente = o.Cliente.IdCliente,
                        Nome = o.Cliente.Nome,
                        Cognome = o.Cliente.Cognome
                    },
                    TotaleOrdine = o.TotaleOrdine,
                    Stato = o.Stato,
                    DataOrdine = o.DataOrdine
                }).ToList();

                // costruttore paginazione
                var viewModel = new PagOrdiniViewModel<OrdineViewModel>
                {
                    Ordini = ordiniViewModel,
                    TotalPages = totalPages,
                    CurrentPage = page,
                    PageSize = pageSize
                };


                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini.");
                return View("Error");
            }
        }

        // Form per creare un nuovo ordine
        public async Task<IActionResult> Create()
        {
            var viewModel = new OrdineViewModel
            {
                DataOrdine = DateTime.Now,
                Clienti = await _context.Clienti
                    .Select(c => new ClienteViewModel
                    {
                        IdCliente = c.IdCliente,
                        Nome = c.Nome,
                        Cognome = c.Cognome
                    }).ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetClientiSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new List<object>()); //returna una lista object vuota
            }

            var clienti = await _context.Clienti
                .Where(c => c.Nome.Contains(term) || c.Cognome.Contains(term))
                .Select(c => new { label = $"{c.Nome} {c.Cognome}", value = c.IdCliente })
                .ToListAsync();

            return Json(clienti);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdOrdine,IdCliente,TotaleOrdine,Stato,DataOrdine")] OrdineViewModel ordineViewModel)
        {
            if (ModelState.ContainsKey("Cliente"))
            {
                ModelState.Remove("Cliente");
            }

            // Ripopola la lista dei clienti
            ordineViewModel.Clienti = await _context.Clienti
                .Select(c => new ClienteViewModel
                {
                    IdCliente = c.IdCliente,
                    Nome = c.Nome,
                    Cognome = c.Cognome
                }).ToListAsync();

            // Verifica se il cliente esiste
            var cliente = await _context.Clienti.FindAsync(ordineViewModel.IdCliente);
            if (cliente == null)
            {
                _logger.LogError($"Cliente con Id {ordineViewModel.IdCliente} non trovato");
                ModelState.AddModelError("IdCliente", "Cliente non trovato");
                return View(ordineViewModel);
            }

            var ordine = new Ordine
            {
                IdCliente = ordineViewModel.IdCliente,
                TotaleOrdine = ordineViewModel.TotaleOrdine,
                Stato = ordineViewModel.Stato,
                DataOrdine = ordineViewModel.DataOrdine
            };

            try
            {
                _context.Ordini.Add(ordine);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ordine creato con successo!";
                _logger.LogInformation($"Ordine creato con successo, ID: {ordine.IdOrdine}");


                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante il salvataggio dell'ordine: {ex.Message}");
                ModelState.AddModelError(string.Empty, "Si è verificato un errore durante il salvataggio dell'ordine.");
                return View(ordineViewModel);
            }
        }



        // GET
        public async Task<IActionResult> Edit(int id)
        {
            _logger.LogInformation($"GET Edit action called for order ID: {id}");

            var ordine = await _ordiniRepository.GetByIdAsync(id);
            if (ordine == null)
            {
                _logger.LogWarning($"Order with ID {id} not found");
                return NotFound();
            }

            // Creiamo un OrdineViewModel per la vista
            var ordineViewModel = new OrdineViewModel
            {
                IdOrdine = ordine.IdOrdine,
                IdCliente = ordine.IdCliente,
                TotaleOrdine = ordine.TotaleOrdine,
                Stato = ordine.Stato,
                DataOrdine = ordine.DataOrdine
            };

            // Carica la lista dei clienti e crea un SelectList
            var clienti = await _context.Clienti
                .Select(c => new { c.IdCliente, NomeCompleto = c.Nome + " " + c.Cognome })
                .ToListAsync();

            // Passiamo la lista di clienti alla vista tramite ViewBag
            ViewBag.Clienti = new SelectList(clienti, "IdCliente", "NomeCompleto", ordineViewModel.IdCliente);

            return View(ordineViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrdineViewModel ordineViewModel)
        {
            if (id != ordineViewModel.IdOrdine)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Verifica se il cliente esiste nel database
                    var clienteExists = await _customerRepository.ExistsAsync(ordineViewModel.IdCliente);
                    if (!clienteExists)
                    {
                        ModelState.AddModelError("IdCliente", "Cliente non trovato.");
                        return View(ordineViewModel);
                    }

                    // Mappa il ViewModel all'entità Ordine
                    var ordine = new Ordine
                    {
                        IdOrdine = ordineViewModel.IdOrdine,
                        IdCliente = ordineViewModel.IdCliente,
                        Stato = ordineViewModel.Stato,
                        TotaleOrdine = ordineViewModel.TotaleOrdine,
                        DataOrdine = ordineViewModel.DataOrdine
                    };

                    await _ordiniRepository.UpdateAsync(ordine);
                    TempData["SuccessMessage"] = "Ordine modificato con successo!";

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante la modifica dell'ordine.");
                    TempData["ErrorMessage"] = $"Si è verificato un errore durante la modifica dell'ordine: {ex.Message}";
                    return View(ordineViewModel);
                }
            }
            return View(ordineViewModel);
        }


        // Elimina un ordine
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _ordiniRepository.DeleteAsync(id);
                _logger.LogInformation($"Ordine con ID {id} eliminato con successo.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione dell'ordine.");
                TempData["Error"] = "Si è verificato un errore durante l'eliminazione dell'ordine.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Funzione per ottenere i clienti (evita duplicazioni)
        private async Task<List<Cliente>> GetClientiAsync()
        {
            try
            {
                var clienti = await _customerRepository.GetAllAsync();
                if (clienti == null || clienti.Count == 0)
                {
                    _logger.LogWarning("Nessun cliente trovato.");
                }
                return clienti ?? new List<Cliente>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei clienti.");
                TempData["Error"] = "Si è verificato un errore durante il recupero dei clienti.";
                return new List<Cliente>();
            }
        }
    }
}
