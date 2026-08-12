using System.Collections.Generic;
using System.Threading.Tasks;
using ArtemisBankingPro.Application.ViewModels.Cliente;

namespace ArtemisBankingPro.Application.Interfaces.Services
{
    public interface ICuentaAhorroService
    {
        Task<List<CuentaAhorroViewModel>> GetActiveCuentasByClientIdAsync(int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarTransferenciaAsync(TransferenciaViewModel model, int clienteId);
    }
}