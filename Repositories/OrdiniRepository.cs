using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.ViewModels;

namespace WebAppEF.Repositories
{
    public class OrdiniRepository(ApplicationDbContext context, ILogger<OrdiniRepository> logger) : IOrdiniRepository
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<OrdiniRepository> _logger = logger;

        // Recupero tutti gli ordini
        public async Task<List<Ordine>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Recupero tutti gli ordini.");
                return await _context.Ordini.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini.");
                throw new Exception("Errore durante il recupero degli ordini.", ex);
            }
        }

        // Recupero ordine per ID
        public async Task<Ordine> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Recupero ordine con ID {id}.");
#pragma warning disable CS8603 // Possible null reference return.
                return await _context.Ordini.FirstOrDefaultAsync(o => o.IdOrdine == id);
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il recupero dell'ordine con ID {id}.");
                throw new Exception($"Errore durante il recupero dell'ordine con ID {id}.", ex);
            }
        }

        // Aggiungi un ordine
        public async Task AddAsync(OrdineViewModel ordineViewModel)
        {
            _logger.LogDebug("Test di log di debug");

            try
            {
                _logger.LogInformation($"Tentativo di salvataggio di un nuovo ordine per il cliente {ordineViewModel.IdCliente}");

                // Trova il cliente nel database
                var cliente = await _context.Clienti.FindAsync(ordineViewModel.IdCliente);
                if (cliente == null)
                {
                    _logger.LogError($"Cliente con Id {ordineViewModel.IdCliente} non trovato.");
                    throw new Exception($"Cliente con Id {ordineViewModel.IdCliente} non esiste.");
                }

                // Mappa i dati del ViewModel all'entità Ordine
                var ordine = new Ordine
                {
                    IdCliente = ordineViewModel.IdCliente,
                    Cliente = cliente,  // Associazione esplicita del Cliente
                    TotaleOrdine = ordineViewModel.TotaleOrdine,
                    Stato = ordineViewModel.Stato,
                    DataOrdine = ordineViewModel.DataOrdine
                };

                _context.Ordini.Add(ordine);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation($"Ordine per il cliente {ordineViewModel.IdCliente} salvato con successo.");
                }
                else
                {
                    _logger.LogWarning($"Ordine per il cliente {ordineViewModel.IdCliente} non è stato salvato.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il salvataggio dell'ordine per il cliente {ordineViewModel.IdCliente}.");
                throw new Exception("Errore durante l'aggiunta dell'ordine.", ex);
            }
        }

        public async Task UpdateAsync(Ordine ordine)
        {
            try
            {
                // Recupera l'ordine esistente
                var existingOrdine = await _context.Ordini.FindAsync(ordine.IdOrdine);
                if (existingOrdine == null)
                {
                    throw new KeyNotFoundException("Ordine non trovato.");
                }

                _logger.LogInformation($"Aggiornamento ordine {ordine.IdOrdine}");

                // Stacca l'entità esistente se è già tracciata
                _context.Entry(existingOrdine).State = EntityState.Detached;

                // Applica i nuovi valori all'entità esistente
                _context.Ordini.Update(ordine);

                // Salva le modifiche nel contesto
                await _context.SaveChangesAsync();
            }
            catch (KeyNotFoundException knfEx)
            {
                _logger.LogWarning(knfEx.Message);
                throw new KeyNotFoundException(knfEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'aggiornamento dell'ordine.");
                throw new Exception("Errore durante l'aggiornamento dell'ordine.", ex);
            }
        }

        // Elimina un ordine
        public async Task DeleteAsync(int id)
        {
            try
            {
                var ordine = await _context.Ordini.FindAsync(id);
                if (ordine == null)
                {
                    throw new KeyNotFoundException("Ordine non trovato.");
                }

                _logger.LogInformation($"Eliminazione ordine {id}");
                _context.Ordini.Remove(ordine);
                await _context.SaveChangesAsync();
            }
            catch (KeyNotFoundException knfEx)
            {
                _logger.LogWarning(knfEx.Message);
                throw new KeyNotFoundException(knfEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'eliminazione dell'ordine.");
                throw new Exception("Errore durante l'eliminazione dell'ordine.", ex);
            }
        }

        // Verifica se l'ordine esiste
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                _logger.LogInformation($"Verifica se esiste l'ordine con ID {id}.");
                return await _context.Ordini.AnyAsync(o => o.IdOrdine == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante il controllo dell'esistenza dell'ordine con ID {id}.");
                throw new Exception($"Errore durante il controllo dell'esistenza dell'ordine con ID {id}.", ex);
            }
        }

        // Nuovo metodo CountAsync
        public async Task<int> CountAsync()
        {
            try
            {
                _logger.LogInformation("Recupero il conteggio degli ordini.");
                return await _context.Ordini.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il conteggio degli ordini.");
                throw new Exception("Errore durante il conteggio degli ordini.", ex);
            }
        }

        // Nuovo metodo GetAllPagedAsync
        public async Task<List<Ordine>> GetAllPaged(int page, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Recupero ordini paginati per la pagina {page} con {pageSize} ordini per pagina.");
                return await _context.Ordini
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Include(o => o.Cliente) // Includi la relazione Cliente qui
                    .ToListAsync(); // Esegui la query e restituisci la lista
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il recupero degli ordini paginati.");
                throw new Exception("Errore durante il recupero degli ordini paginati.", ex);
            }
        }



        public Task AddAsync(Ordine ordine)
        {
            throw new NotImplementedException();
        }
    }
}
