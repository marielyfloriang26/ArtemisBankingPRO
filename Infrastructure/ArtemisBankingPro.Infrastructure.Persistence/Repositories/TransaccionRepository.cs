using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class TransaccionRepository : GenericRepository<Transaccion>, ITransaccionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TransaccionRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Transaccion>> GetByCuentaIdAsync(int cuentaId)
    {
        return await _dbContext.Transacciones
            .Where(t => t.CuentaOrigenId == cuentaId || t.CuentaDestinoId == cuentaId)
            .OrderByDescending(t => t.FechaTransaccion)
            .ToListAsync();
    }
}
