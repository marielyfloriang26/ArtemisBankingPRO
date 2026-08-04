using ArtemisBankingPro.Domain.Entities;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface IBeneficiarioRepository : IGenericRepository<Beneficiario>
{
    Task<List<Beneficiario>> GetAllByClienteIdAsync(int clienteId);
    Task<Beneficiario?> GetByClienteAndCuentaIdAsync(int clienteId, int cuentaAhorroId);
}
