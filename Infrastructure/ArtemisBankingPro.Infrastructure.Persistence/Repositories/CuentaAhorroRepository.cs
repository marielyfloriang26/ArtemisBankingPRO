using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class CuentaAhorroRepository : GenericRepository<CuentaAhorro>, ICuentaAhorroRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CuentaAhorroRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CuentaAhorro>> GetByClienteIdAsync(int clienteId)
    {
        return await _dbContext.CuentasAhorro
            .Where(c => c.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<CuentaAhorro?> GetByNumeroCuentaAsync(string numeroCuenta)
    {
        return await _dbContext.CuentasAhorro
            .FirstOrDefaultAsync(c => c.NumeroCuenta == numeroCuenta);
    }

    public async Task<CuentaAhorro?> GetCuentaPrincipalByUsuarioIdAsync(int usuarioId)
    {
        return await _dbContext.CuentasAhorro
            .FirstOrDefaultAsync(c => c.ClienteId == usuarioId && c.TipoCuenta == "Principal");
    }
}
