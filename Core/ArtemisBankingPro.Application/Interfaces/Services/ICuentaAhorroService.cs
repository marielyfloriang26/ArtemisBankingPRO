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
        Task<(List<(CuentaAhorro Cuenta, Usuario? Cliente)> Cuentas, int TotalRegistros)> GetAllPaginatedAsync(int page, int pageSize, string? identification, string status, string type);
        Task<(bool Success, string ErrorMessage, CuentaAhorro? CuentaCreada, Usuario? Cliente)> CreateSecondaryAccountAsync(int clienteId, decimal balanceInicial, int adminId);
        Task<(CuentaAhorro? Cuenta, Usuario? Cliente, List<Transaccion> Transacciones, int TotalRegistros)> GetTransaccionesByAccountAsync(string numeroCuenta, int page, int pageSize);

        Task<(bool Success, string ErrorMessage)> CancelSecondaryAccountAsync(string numeroCuenta, int adminId);

        Task<List<ArtemisBankingPro.Application.ViewModels.Cliente.TransaccionDetalleViewModel>> GetTransaccionesByCuentaIdAsync(int cuentaId, int clienteId);

    }
}