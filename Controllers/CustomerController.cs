using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.Repositories;
using WebAppEF.Utilities;
using WebAppEF.ViewModel;

namespace WebAppEF.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerRepository customerRepository,
            ILogger<CustomerController> logger)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;
            
            try
            {
                _logger.LogInformation("Recupero clienti - Pagina {Page}", page);
                
                var totalItems = await _customerRepository.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                var clienti = await _customerRepository.GetAllPagedAsync(page, pageSize);

                var viewModel = new PaginazioneViewModel 
                { 
                    Clienti = clienti, 
                    CurrentPage = page, 
                    TotalPages = totalPages 
                };
                
                _logger.LogInformation("Clienti recuperati con successo: {TotalItems} totali, {TotalPages} pagine", 
                    totalItems, totalPages);
                
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            try
            {
                await _customerRepository.AddAsync(cliente);
                _logger.LogInformation("Cliente creato con successo: ID {Id}", cliente.IdCliente);

                TempData["SuccessMessage"] = "Cliente creato correttamente!";
                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Errore di validazione durante la creazione del cliente");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(cliente);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante la creazione del cliente");
                ModelState.AddModelError("Email", ex.Message);
                return View(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la creazione del cliente");
                TempData["ErrorMessage"] = "Si è verificato un errore durante la creazione del cliente.";
                return View(cliente);
            }
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

            try
            {
                bool exists = await _customerRepository.EmailExistsAsync(email);
                if (exists)
                {
                    return Json(new { valid = false, message = "Questa email è già registrata." });
                }

                return Json(new { valid = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il controllo dell'email");
                return Json(new { valid = false, message = "Errore durante il controllo dell'email." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                _logger.LogInformation("Accesso al form di modifica per cliente con ID {Id}", id);
                
                var customer = await _customerRepository.GetByIdAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Cliente non trovato per ID {Id}", id);
                    return NotFound();
                }

                return View(new ModificaClienteViewModel 
                { 
                    IdCliente = customer.IdCliente, 
                    Nome = customer.Nome, 
                    Cognome = customer.Cognome, 
                    Email = customer.Email, 
                    Attivo = customer.Attivo 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero del cliente con ID {Id}", id);
                TempData["ErrorMessage"] = "Errore durante il recupero del cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ModificaClienteViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Errore nel ModelState per modifica cliente {IdCliente}", viewModel.IdCliente);
                return View(viewModel);
            }

            try
            {
                var customer = new Cliente
                {
                    IdCliente = viewModel.IdCliente,
                    Nome = viewModel.Nome,
                    Cognome = viewModel.Cognome,
                    Email = viewModel.Email,
                    Attivo = viewModel.Attivo
                };

                await _customerRepository.UpdateAsync(customer);
                
                _logger.LogInformation("Cliente aggiornato con successo: ID {IdCliente}", viewModel.IdCliente);
                TempData["SuccessMessage"] = "Cliente aggiornato con successo!";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Cliente non trovato per modifica: ID {IdCliente}", viewModel.IdCliente);
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Errore di validazione durante l'aggiornamento del cliente");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Operazione non valida durante l'aggiornamento del cliente");
                ModelState.AddModelError("Email", ex.Message);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento del cliente {IdCliente}", viewModel.IdCliente);
                TempData["ErrorMessage"] = "Errore durante l'aggiornamento del cliente.";
                return View(viewModel);
            }
        }

        [HttpGet]
       
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Tentativo di eliminazione cliente con ID {Id}", id);
                
                await _customerRepository.DeleteAsync(id);
                
                _logger.LogInformation("Cliente con ID {Id} eliminato con successo", id);
                TempData["SuccessMessage"] = "Cliente eliminato con successo!";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Cliente non trovato per eliminazione: ID {Id}", id);
                TempData["ErrorMessage"] = "Cliente non trovato.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione del cliente con ID {Id}", id);
                TempData["ErrorMessage"] = "Errore durante l'eliminazione del cliente.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}