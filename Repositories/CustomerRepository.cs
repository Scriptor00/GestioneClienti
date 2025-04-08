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
        private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<List<Cliente>> GetAllAsync()
        {
            return await _context.Clienti
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Cliente> GetByIdAsync(int id)
        {
            return await _context.Clienti
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCliente == id);
        }

        public async Task AddAsync(Cliente cliente)
        {
            if (cliente == null)
                throw new ArgumentNullException(nameof(cliente));

            if (!ValidazioneCliente.IsValidEmail(cliente.Email))
                throw new ArgumentException("Email non valida", nameof(cliente.Email));

            if (!ValidazioneCliente.LunghezzaMail(cliente.Email))
                throw new ArgumentException("L'email deve essere tra 5 e 255 caratteri", nameof(cliente.Email));

            if (await EmailExistsAsync(cliente.Email))
                throw new InvalidOperationException("Email già esistente");

            if (ValidazioneCliente.HasSpecialCharacters(cliente.Nome) || 
               ValidazioneCliente.HasSpecialCharacters(cliente.Cognome))
                throw new ArgumentException("Nome/Cognome contiene caratteri speciali non validi");

            await _context.Clienti.AddAsync(cliente);
            await _context.SaveChangesAsync();
        }

       public async Task UpdateAsync(Cliente cliente)
        {
            var existingCliente = await _context.Clienti.FindAsync(cliente.IdCliente);
            if (existingCliente == null)
                throw new KeyNotFoundException("Cliente non trovato");

            if (!ValidazioneCliente.IsValidEmail(cliente.Email))
                throw new ArgumentException("Email non valida", nameof(cliente.Email));

            if (!ValidazioneCliente.LunghezzaMail(cliente.Email))
                throw new ArgumentException("L'email deve essere tra 5 e 255 caratteri", nameof(cliente.Email));

            if (await _context.Clienti.AnyAsync(c => c.Email == cliente.Email && c.IdCliente != cliente.IdCliente))
                throw new InvalidOperationException("Email già utilizzata da un altro cliente");

            if (ValidazioneCliente.HasSpecialCharacters(cliente.Nome) ||
                ValidazioneCliente.HasSpecialCharacters(cliente.Cognome))
                throw new ArgumentException("Nome/Cognome contiene caratteri speciali non validi");

            existingCliente.Nome = cliente.Nome;
            existingCliente.Cognome = cliente.Cognome;
            existingCliente.Email = cliente.Email;
            existingCliente.Attivo = cliente.Attivo;
            existingCliente.Indirizzo = cliente.Indirizzo;
            existingCliente.Civico = cliente.Civico;
            existingCliente.Citta = cliente.Citta;
            existingCliente.Cap = cliente.Cap;
            existingCliente.Paese = cliente.Paese;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var cliente = await _context.Clienti.FindAsync(id);
            if (cliente == null)
                throw new KeyNotFoundException("Cliente non trovato");

            _context.Clienti.Remove(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Clienti.AnyAsync(e => e.IdCliente == id);
        }

        public async Task<List<Cliente>> GetAllPagedAsync(int page, int pageSize)
        {
            return await _context.Clienti
                .OrderBy(c => c.IdCliente)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _context.Clienti.CountAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Clienti.AnyAsync(c => c.Email == email);
        }
    }
}