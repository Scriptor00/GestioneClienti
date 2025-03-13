using System.Collections.Generic;
using System.Threading.Tasks;
using WebAppEF.Entities;
using Microsoft.EntityFrameworkCore;
using WebAppEF.Models;

namespace WebAppEF.Repositories
{
    public interface ICustomerRepository
    {
        Task<List<Cliente>> GetAllAsync();
        Task<Cliente> GetByIdAsync(int id);
        Task AddAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

         Task<List<Cliente>> GetAllPagedAsync(int page, int pageSize); 
        Task<int> CountAsync(); 
    }

   
}
