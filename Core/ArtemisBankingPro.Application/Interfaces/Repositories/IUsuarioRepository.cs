using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface IUsuarioRepository : IGenericRepository<Usuario>
{
    Task<Usuario?> GetByCedulaAsync(string cedula);
    Task<List<Usuario>> GetAllClientesAsync();
}
