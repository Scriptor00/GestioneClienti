using System;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.Repositories;
using WebAppEF.Utilities;
using WebAppEF.ViewModel;
using WebAppEF.ViewModels;

namespace WebAppEF.Controllers
{
    public class CustomerController(ApplicationDbContext context, ICustomerRepository customerRepository, ILogger<CustomerController> logger) : Controller
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<CustomerController> _logger = logger;

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;
            try
            {
                _logger.LogInformation("Recupero clienti - Pagina {Page}", page);
                int totalItems = await _customerRepository.CountAsync();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var clienti = await _customerRepository.GetAllPagedAsync(page, pageSize);

                var viewModel = new PaginazioneViewModel { Clienti = clienti, CurrentPage = page, TotalPages = totalPages };
                _logger.LogInformation("Clienti recuperati con successo: {TotalItems} totali, {TotalPages} pagine", totalItems, totalPages);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero dei clienti");
                TempData["ErrorMessage"] = "Errore durante il recupero dei clienti.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Create()
        {
            _logger.LogInformation("Accesso al form di creazione cliente");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            // Controllo email
            if (!ValidazioneCliente.IsValidEmail(cliente.Email))
            {
                ModelState.AddModelError("Email", "L'email fornita non è valida.");
                return View(cliente);
            }

            if (!ValidazioneCliente.LunghezzaMail(cliente.Email))
            {
                ModelState.AddModelError("Email", "L'email deve essere tra 5 e 255 caratteri.");
                return View(cliente);
            }

            bool emailExists = await _context.Clienti.AnyAsync(c => c.Email == cliente.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Questa email è già registrata.");
                return View(cliente);
            }

            if (ValidazioneCliente.HasSpecialCharacters(cliente.Nome) || ValidazioneCliente.HasSpecialCharacters(cliente.Cognome))
            {
                ModelState.AddModelError("Nome", "Il nome non può contenere caratteri speciali.");
                ModelState.AddModelError("Cognome", "Il cognome non può contenere caratteri speciali.");
                return View(cliente);
            }

            try
            {
                await _customerRepository.AddAsync(cliente);
                _logger.LogInformation("Cliente creato con successo: {Cliente}", cliente);

                TempData["SuccessMessage"] = "Cliente salvato correttamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del cliente");
                ModelState.AddModelError("", "Errore durante il salvataggio.");
            }

            return View(cliente);
        }



        [HttpGet]
        public async Task<JsonResult> CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { valid = false, message = "L'email è obbligatoria." });
            }

            if (!ValidazioneCliente.LunghezzaMail(email))
            {
                return Json(new { valid = false, message = "L'email deve essere tra 5 e 255 caratteri." });
            }

            if (!ValidazioneCliente.IsValidEmail(email))
            {
                return Json(new { valid = false, message = "L'email non è valida." });
            }

            bool exists = await _context.Clienti.AnyAsync(c => c.Email == email);
            if (exists)
            {
                return Json(new { valid = false, message = "Questa email è già registrata." });
            }

            return Json(new { valid = true });
        }



        [HttpGet]
        public IActionResult Edit(int id)
        {
            _logger.LogInformation("Accesso al form di modifica per cliente con ID {Id}", id);
            var customer = _context.Clienti.Find(id);
            if (customer == null)
            {
                _logger.LogWarning("Cliente non trovato per ID {Id}", id);
                return NotFound();
            }
            return View(new ModificaClienteViewModel { IdCliente = customer.IdCliente, Nome = customer.Nome, Cognome = customer.Cognome, Email = customer.Email, Attivo = customer.Attivo });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ModificaClienteViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Errore nel ModelState per modifica cliente {IdCliente}", viewModel.IdCliente);
                TempData["ErrorMessage"] = "Errore durante l'aggiornamento.";
                return View(viewModel);
            }

            var customer = _context.Clienti.Find(viewModel.IdCliente);
            if (customer == null)
            {
                _logger.LogWarning("Cliente non trovato per modifica: ID {IdCliente}", viewModel.IdCliente);
                return NotFound();
            }

            customer.Nome = viewModel.Nome;
            customer.Cognome = viewModel.Cognome;
            customer.Email = viewModel.Email;
            customer.Attivo = viewModel.Attivo;
            _context.SaveChanges();

            _logger.LogInformation("Cliente aggiornato con successo: {IdCliente}", customer.IdCliente);
            TempData["SuccessMessage"] = "Cliente aggiornato con successo!";
            return View(viewModel);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Tentativo di eliminazione cliente con ID {Id}", id);
                var cliente = await _customerRepository.GetByIdAsync(id);
                if (cliente == null)
                {
                    _logger.LogWarning("Cliente non trovato per eliminazione: ID {Id}", id);
                    TempData["ErrorMessage"] = "Cliente non trovato.";
                    return RedirectToAction(nameof(Index));
                }

                await _customerRepository.DeleteAsync(id);
                _logger.LogInformation("Cliente con ID {Id} eliminato con successo", id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del cliente con ID {Id}", id);
                TempData["ErrorMessage"] = "Errore durante l'eliminazione.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}