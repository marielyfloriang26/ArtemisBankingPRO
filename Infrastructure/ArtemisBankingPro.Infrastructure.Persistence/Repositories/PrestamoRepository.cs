using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class PrestamoRepository : GenericRepository<Prestamo>, IPrestamoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PrestamoRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Prestamo>> GetAllWithIncludesAsync()
    {
        return await _dbContext.Prestamos
            .Include(p => p.Cliente)
            .Include(p => p.Cuotas)
            .ToListAsync();
    }

    public async Task<Prestamo?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbContext.Prestamos
            .Include(p => p.Cliente)
            .Include(p => p.Cuotas)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Prestamo>> GetByClienteIdAsync(int clienteId)
    {
        return await _dbContext.Prestamos
            .Include(p => p.Cuotas)
            .Where(p => p.ClienteId == clienteId)
            .ToListAsync();
    }

    public async Task<Prestamo?> GetPrestamoActivoByClienteIdAsync(int clienteId)
    {
        return await _dbContext.Prestamos
            .Where(p => p.ClienteId == clienteId && p.Estado == "Activo")
            .FirstOrDefaultAsync();
    }

    public async Task<Prestamo?> GetByNumeroPrestamoAsync(string numeroPrestamo)
    {
        return await _dbContext.Prestamos
            .FirstOrDefaultAsync(p => p.NumeroPrestamo == numeroPrestamo);
    }
}
