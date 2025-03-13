using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Entities;
using WebAppEF.Models;
using WebAppEF.Utilities;

namespace WebAppEF.Repositories
{
    public class CustomerRepository(ApplicationDbContext context) : ICustomerRepository
    {
        private readonly ApplicationDbContext _context = context;

        // lista di tutti i clienti
        public async Task<List<Cliente>> GetAllAsync()
        {
            try
            {
                return await _context.Clienti.AsNoTracking().ToListAsync();  
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante il recupero dei clienti.", ex);
            }
        }

        // Recupero un cliente per ID
        public async Task<Cliente> GetByIdAsync(int id)
        {
            try
            {
#pragma warning disable CS8603 // Possible null reference return.
                return await _context.Clienti.AsNoTracking().FirstOrDefaultAsync(c => c.IdCliente == id);
#pragma warning restore CS8603 // Possible null reference return.
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il recupero del cliente con ID {id}.", ex);
            }
        }

        // Aggiungo un nuovo cliente
        public async Task AddAsync(Cliente cliente)
   {
    if (cliente is null)
        throw new ArgumentNullException(nameof(cliente), "Il cliente deve esistere.");

    if (await _context.Clienti.AnyAsync(c => c.Email == cliente.Email))
        throw new InvalidOperationException("Un cliente con questa email esiste già.");
    
    if(ValidazioneCliente.LunghezzaMail(cliente.Email) == false)
       throw new ArgumentException("La lunghezza della mail deve essere tra 5 e 255 caratteri");

    if (!ValidazioneCliente.IsValidEmail(cliente.Email))
        throw new ArgumentException("L'email fornita non è valida.", nameof(cliente.Email));

    if (ValidazioneCliente.HasSpecialCharacters(cliente.Nome) || 
        ValidazioneCliente.HasSpecialCharacters(cliente.Cognome))
        throw new ArgumentException("Nome e cognome non possono contenere caratteri speciali.");

    _context.Clienti.Add(cliente);
    await _context.SaveChangesAsync();
    }


        // Aggiorno un cliente
       public async Task UpdateAsync(Cliente cliente)
   {
    try
    {
        // Recupera il cliente esistente dal database
        var clienteEsistente = await _context.Clienti.FindAsync(cliente.IdCliente);
        if (clienteEsistente == null)
        {
            throw new KeyNotFoundException("Cliente non trovato.");
        }

        // Controlla se esiste già un altro cliente con la stessa email
        var emailEsistente = await _context.Clienti
            .Where(c => c.IdCliente != cliente.IdCliente && c.Email == cliente.Email)
            .AnyAsync();

        if (emailEsistente)
        {
            throw new Exception("Un altro cliente con questa email esiste già.");
        }

        // Aggiorno il cliente nel db
        clienteEsistente.Nome = cliente.Nome;
        clienteEsistente.Cognome = cliente.Cognome;
        clienteEsistente.Email = cliente.Email;
        clienteEsistente.Attivo = cliente.Attivo;

       await _context.SaveChangesAsync();
    }
    catch (KeyNotFoundException)
    {
        throw; // Rilancia la KeyNotFoundException per gestirla a livello di controller
    }
    catch (Exception ex)
    {
        throw new Exception("Errore durante l'aggiornamento del cliente.", ex);
    }
   }

       // Elimina un cliente
        public async Task DeleteAsync(int id)
        {
            try
            {
                var cliente = await _context.Clienti.FindAsync(id);
                if (cliente == null)
                {
                    throw new KeyNotFoundException("Cliente non trovato.");
                }

                _context.Clienti.Remove(cliente);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Errore durante l'eliminazione del cliente.", ex);
            }
        }

        // Verifica se il cliente esiste
        public async Task<bool> ExistsAsync(int id)
        {
            try
            {
                return await _context.Clienti.AnyAsync(e => e.IdCliente == id);
            }
            catch (Exception ex)
            {
                throw new Exception($"Errore durante il controllo dell'esistenza del cliente con ID {id}.", ex);
            }
        }

        public async Task<List<Cliente>> GetAllPagedAsync(int page, int pageSize)
         {
         return await _context.Clienti
        .OrderBy(c => c.IdCliente) 
        .Skip((page - 1) * pageSize) 
        .Take(pageSize) 
        .ToListAsync();
        }

      public async Task<int> CountAsync()
      {
        return await _context.Clienti.CountAsync();
      }
    }
}
