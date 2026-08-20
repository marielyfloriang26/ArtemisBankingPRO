using System.Collections.Generic;
using System.Threading.Tasks;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using ArtemisBankingPro.Domain.Entities;

namespace ArtemisBankingPro.Application.Interfaces.Services
{
    public interface ICuentaAhorroService
    {
        Task<List<CuentaAhorroViewModel>> GetActiveCuentasByClientIdAsync(int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarTransferenciaAsync(TransferenciaViewModel model, int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarDepositoAsync(ViewModels.Cajero.DepositoViewModel model, int cajeroId);

        Task<(bool Success, string ErrorMessage)> RealizarRetiroAsync(ViewModels.Cajero.RetiroViewModel model, int cajeroId);

        // api
        Task<(List<CuentaAhorro> Cuentas, int TotalRegistros)> GetAllPaginatedAsync(int page, int pageSize, string? identification, string status, string type);

        Task<(bool Success, string ErrorMessage, CuentaAhorro? CuentaCreada)> CreateSecondaryAccountAsync(int clienteId, decimal balanceInicial, int adminId);

        Task<(CuentaAhorro? Cuenta, List<Transaccion> Transacciones, int TotalRegistros)> GetTransaccionesByAccountAsync(string numeroCuenta, int page, int pageSize);

        Task<(bool Success, string ErrorMessage)> CancelSecondaryAccountAsync(string numeroCuenta, int adminId);
    }
}