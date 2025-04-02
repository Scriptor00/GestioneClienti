using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestioneClienti.Entities;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models;

namespace GestioneClienti.Repositories
{
    public class NotificaRepository : INotificaRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Implementazione del metodo per ottenere tutte le notifiche
        public async Task<IEnumerable<Notifica>> GetAllNotificheAsync()
        {
            return await _context.Notifiche.ToListAsync(); // Puoi aggiungere filtri se necessario
        }

        // Implementazione del metodo per ottenere una notifica per ID
        public async Task<Notifica> GetNotificaByIdAsync(int id)
        {
            return await _context.Notifiche.FindAsync(id);
        }

        // Implementazione del metodo per ottenere le notifiche non lette
        public async Task<IEnumerable<Notifica>> GetNotificheNonLetteAsync()
        {
            return await _context.Notifiche.Where(n => !n.Letta).ToListAsync();
        }

        // Implementazione del metodo per segnare una notifica come letta
        public async Task MarkAsReadAsync(int id)
        {
            var notifica = await _context.Notifiche.FindAsync(id);
            if (notifica != null)
            {
                notifica.Letta = true;
                await _context.SaveChangesAsync();
            }
        }

        // Implementazione del metodo per eliminare una notifica
        public async Task DeleteNotificationAsync(int id)
        {
            var notifica = await _context.Notifiche.FindAsync(id);
            if (notifica != null)
            {
                _context.Notifiche.Remove(notifica);
                await _context.SaveChangesAsync();
            }
        }
    }
}
