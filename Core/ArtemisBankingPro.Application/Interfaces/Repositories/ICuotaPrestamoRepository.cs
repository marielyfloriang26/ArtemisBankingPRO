using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface ICuotaPrestamoRepository : IGenericRepository<CuotaPrestamo>
{
    Task<List<CuotaPrestamo>> GetByPrestamoIdAsync(int prestamoId);
}
