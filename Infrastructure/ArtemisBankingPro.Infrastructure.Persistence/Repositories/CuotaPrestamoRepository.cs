using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class CuotaPrestamoRepository : GenericRepository<CuotaPrestamo>, ICuotaPrestamoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CuotaPrestamoRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CuotaPrestamo>> GetByPrestamoIdAsync(int prestamoId)
    {
        return await _dbContext.CuotasPrestamo
            .Where(c => c.PrestamoId == prestamoId)
            .OrderBy(c => c.NumeroCuota)
            .ToListAsync();
    }
}
