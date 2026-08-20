using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface IConsumoTarjetaRepository : IGenericRepository<ConsumoTarjeta>
{
    Task<List<ConsumoTarjeta>> GetByTarjetaIdAsync(int tarjetaId);
}
