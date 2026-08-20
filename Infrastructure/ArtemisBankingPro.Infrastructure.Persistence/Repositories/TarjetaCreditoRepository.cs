using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class TarjetaCreditoRepository : GenericRepository<TarjetaCredito>, ITarjetaCreditoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TarjetaCreditoRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TarjetaCredito>> GetByClienteIdAsync(int clienteId)
    {
        return await _dbContext.TarjetasCredito
            .Where(t => t.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<TarjetaCredito?> GetByNumeroTarjetaAsync(string numeroTarjeta)
    {
        return await _dbContext.TarjetasCredito
            .Include(t => t.Cliente)
            .FirstOrDefaultAsync(t => t.NumeroTarjeta == numeroTarjeta);
    }
}
