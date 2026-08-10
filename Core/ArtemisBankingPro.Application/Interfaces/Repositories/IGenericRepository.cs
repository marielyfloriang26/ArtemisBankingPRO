using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(int id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity, int id);
        Task DeleteAsync(T entity);
    }
}