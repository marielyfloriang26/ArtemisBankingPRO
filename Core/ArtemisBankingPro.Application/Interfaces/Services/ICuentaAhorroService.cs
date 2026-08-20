using System.Collections.Generic;
using System.Threading.Tasks;
using ArtemisBankingPro.Application.ViewModels.Cliente;

namespace ArtemisBankingPro.Application.Interfaces.Services
{
    public interface ICuentaAhorroService
    {
        Task<List<CuentaAhorroViewModel>> GetActiveCuentasByClientIdAsync(int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarTransferenciaAsync(TransferenciaViewModel model, int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarDepositoAsync(ViewModels.Cajero.DepositoViewModel model, int cajeroId);

        Task<(bool Success, string ErrorMessage)> RealizarRetiroAsync(ViewModels.Cajero.RetiroViewModel model, int cajeroId);

        Task<List<ArtemisBankingPro.Application.ViewModels.Cliente.TransaccionDetalleViewModel>> GetTransaccionesByCuentaIdAsync(int cuentaId, int clienteId);
    }
}