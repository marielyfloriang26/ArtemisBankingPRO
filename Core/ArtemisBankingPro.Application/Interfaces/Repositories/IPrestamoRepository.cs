using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface IPrestamoRepository : IGenericRepository<Prestamo>
{
    Task<List<Prestamo>> GetAllWithIncludesAsync();
    Task<Prestamo?> GetByIdWithIncludesAsync(int id);
    Task<List<Prestamo>> GetByClienteIdAsync(int clienteId);
    Task<Prestamo?> GetPrestamoActivoByClienteIdAsync(int clienteId);
    Task<Prestamo?> GetByNumeroPrestamoAsync(string numeroPrestamo);
}
