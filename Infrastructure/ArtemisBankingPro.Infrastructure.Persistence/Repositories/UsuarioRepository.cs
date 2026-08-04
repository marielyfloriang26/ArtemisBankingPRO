using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Repositories;

public class UsuarioRepository : GenericRepository<Usuario>, IUsuarioRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UsuarioRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Usuario?> GetByCedulaAsync(string cedula)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Cedula == cedula);
    }

    public async Task<List<Usuario>> GetAllClientesAsync()
    {
        return await _dbContext.Users
            .Where(u => u.TipoUsuario == "Cliente")
            .ToListAsync();
    }
}
