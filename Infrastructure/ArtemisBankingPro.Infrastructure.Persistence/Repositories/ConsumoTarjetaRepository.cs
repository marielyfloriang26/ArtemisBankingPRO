using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class ConsumoTarjetaRepository : GenericRepository<ConsumoTarjeta>, IConsumoTarjetaRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ConsumoTarjetaRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ConsumoTarjeta>> GetByTarjetaIdAsync(int tarjetaId)
    {
        return await _dbContext.ConsumosTarjeta
            .Where(c => c.TarjetaId == tarjetaId)
            .ToListAsync();
    }
}
