using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface ITransaccionRepository : IGenericRepository<Transaccion>
{
    Task<List<Transaccion>> GetByCuentaIdAsync(int cuentaId);
}
