using ArtemisBankingPro.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Repositories;

public interface ICuentaAhorroRepository : IGenericRepository<CuentaAhorro>
{
    Task<List<CuentaAhorro>> GetByClienteIdAsync(int clienteId);
    Task<CuentaAhorro?> GetByNumeroCuentaAsync(string numeroCuenta);
    Task<CuentaAhorro?> GetCuentaPrincipalByUsuarioIdAsync(int usuarioId);
}
