using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class BeneficiarioRepository : GenericRepository<Beneficiario>, IBeneficiarioRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BeneficiarioRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Beneficiario>> GetAllByClienteIdAsync(int clienteId)
    {
        return await _dbContext.Set<Beneficiario>()
            .Include(b => b.CuentaAhorro)
            .Where(b => b.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<Beneficiario?> GetByClienteAndCuentaIdAsync(int clienteId, int cuentaAhorroId)
    {
        return await _dbContext.Set<Beneficiario>()
            .FirstOrDefaultAsync(b => b.ClienteId == clienteId && b.CuentaAhorroId == cuentaAhorroId);
    }
}
