using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GestioneClienti.Entities;

namespace GestioneClienti.Repositories
{
    public interface INotificaRepository
    {
        Task<IEnumerable<Notifica>> GetAllNotificheAsync();
        Task<Notifica> GetNotificaByIdAsync(int id);
        Task<IEnumerable<Notifica>> GetNotificheNonLetteAsync();
        Task MarkAsReadAsync(int id);
        Task DeleteNotificationAsync(int id);
    }
}