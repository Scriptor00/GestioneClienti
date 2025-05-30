using WebAppEF.Entities;

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
        Task<bool> EmailExistsAsync(string email);
    }
}