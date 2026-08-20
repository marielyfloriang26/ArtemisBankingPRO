using System.Collections.Generic;
using System.Threading.Tasks;
using ArtemisBankingPro.Application.ViewModels.Cliente;

namespace ArtemisBankingPro.Application.Interfaces.Services
{
    public interface ITarjetaCreditoService
    {
        /// Obtiene las tarjetas de credito activas asociadas a un cliente especifico
        Task<List<TarjetaCreditoViewModel>> GetActiveCardsByClientIdAsync(int clienteId);

        // Procesa la transaccion de avance de efectivo
        Task<(bool Success, string ErrorMessage)> RealizarAvanceEfectivoAsync(AvanceEfectivoViewModel model, int clienteId);

        Task<(bool Success, string ErrorMessage)> RealizarPagoAsync(ViewModels.Cajero.PagoTarjetaViewModel model, int cajeroId);


        // api 
        Task<(bool Success, string Message, object? Data)> GetCreditCardsPagedAsync(int page, int pageSize, string status, string? identification);
        Task<(bool Success, string Message, object? Data)> AssignCreditCardAsync(string clientId, decimal creditLimit, int adminUserId);
        Task<(bool Success, string Message, object? Data)> GetCreditCardDetailsAsync(string id);
        Task<(bool Success, string Message)> UpdateCreditLimitAsync(string id, decimal newLimit);
        Task<(bool Success, string Message)> CancelCreditCardAsync(string id);

        // Métodos para el módulo "Gestión de tarjetas de crédito" del Administrador
        Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.TarjetaAdminViewModel>> GetAllTarjetasFilteredAsync(string? cedula, string estadoFiltro);
        Task<List<ArtemisBankingPro.Application.ViewModels.Prestamos.ClienteElegibleViewModel>> GetClientesElegiblesAsync(string? cedula);
        Task<decimal> CalcularDeudaPromedioGlobalAsync();
        Task<string> AsignarTarjetaAsync(ArtemisBankingPro.Application.ViewModels.AdminTarjeta.AssignTarjetaViewModel model);
        Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel>> GetConsumosTarjetaAsync(int tarjetaId);
        Task<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.EditLimiteTarjetaViewModel?> GetEditLimiteViewModelAsync(int id);
        Task<string> EditLimiteAsync(ArtemisBankingPro.Application.ViewModels.AdminTarjeta.EditLimiteTarjetaViewModel model);
        Task<string> CancelTarjetaAsync(int id);
        Task<bool> ExisteClienteConCedulaAsync(string cedula);
        Task<List<ArtemisBankingPro.Application.ViewModels.AdminTarjeta.ConsumoTarjetaViewModel>> GetConsumosByTarjetaIdAsync(int tarjetaId, int clienteId);

    }
}