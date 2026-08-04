using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface ITarjetaCreditoRepository : IGenericRepository<TarjetaCredito>
{
    Task<List<TarjetaCredito>> GetByClienteIdAsync(int clienteId);
}
