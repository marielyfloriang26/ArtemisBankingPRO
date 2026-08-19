using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface IComercioRepository : IGenericRepository<Comercio>
{
    Task<List<Comercio>> GetAllWithIncludesAsync();
    Task<Comercio?> GetByIdWithIncludesAsync(int id);
    Task<bool> ExisteRncAsync(string rnc, int? excluirComercioId = null);
    Task<bool> ExisteCorreoAsync(string correo, int? excluirComercioId = null);
    Task<List<Usuario>> GetUsuariosAsociadosAsync(int comercioId);
}
