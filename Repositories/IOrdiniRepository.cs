using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAppEF.Entities;
using WebAppEF.Models;

namespace WebAppEF.Repositories
{
    public interface IOrdiniRepository
    {
        Task<List<Ordine>> GetAllAsync();
        Task<Ordine> GetByIdAsync(int id);
        Task AddAsync(Ordine ordine);
        Task UpdateAsync(Ordine ordine);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        Task<int> CountAsync(); 
    Task<List<Ordine>> GetAllPaged(int page, int pageSize);
    }
}
