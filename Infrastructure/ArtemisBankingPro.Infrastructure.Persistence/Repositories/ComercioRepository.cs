using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class ComercioRepository : GenericRepository<Comercio>, IComercioRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ComercioRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Comercio>> GetAllWithIncludesAsync()
    {
        return await _dbContext.Comercios
            .AsNoTracking()
            .Include(c => c.ComercioUsuarioRel)
                .ThenInclude(cu => cu!.Usuario)
            .ToListAsync();
    }

    public async Task<Comercio?> GetByIdWithIncludesAsync(int id)
    {
        return await _dbContext.Comercios
            .AsNoTracking()
            .Include(c => c.ComercioUsuarioRel)
                .ThenInclude(cu => cu!.Usuario)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExisteRncAsync(string rnc, int? excluirComercioId = null)
    {
        return await _dbContext.Comercios
            .AnyAsync(c => c.RNC == rnc && (excluirComercioId == null || c.Id != excluirComercioId));
    }

    public async Task<bool> ExisteCorreoAsync(string correo, int? excluirComercioId = null)
    {
        return await _dbContext.Comercios
            .AnyAsync(c => c.Correo == correo && (excluirComercioId == null || c.Id != excluirComercioId));
    }

    public async Task<List<Usuario>> GetUsuariosAsociadosAsync(int comercioId)
    {
        return await _dbContext.ComercioUsuarios
            .Where(cu => cu.ComercioId == comercioId && cu.Usuario != null)
            .Select(cu => cu.Usuario!)
            .ToListAsync();
    }
}
