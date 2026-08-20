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
    }
}