using ArtemisBankingPro.Application.ViewModels.Cajero;
using ArtemisBankingPro.Application.ViewModels.Transacciones;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface ITransaccionCajeroService
{
    // Pago a préstamo
    Task<(string? error, PagoPrestamoCajeroConfirmViewModel? confirm)> PreviewPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroFormViewModel model);
    Task<OperationResultViewModel> EjecutarPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroConfirmViewModel model);

    // Transacciones a cuentas de terceros
    Task<(string? error, TransaccionTercerosCajeroConfirmViewModel? confirm)> PreviewTransaccionTercerosAsync(int cajeroId, TransaccionTercerosCajeroFormViewModel model);
    Task<OperationResultViewModel> EjecutarTransaccionTercerosAsync(int cajeroId, TransaccionTercerosCajeroConfirmViewModel model);

    Task<(string? error, DepositoViewModel? confirm)> PreviewDepositoAsync(int cajeroId, DepositoViewModel model);
    Task<OperationResultViewModel> EjecutarDepositoAsync(int cajeroId, DepositoViewModel model);
    Task<(int transaccionesHoy, int pagosHoy, int depositosHoy, int retirosHoy)> GetIndicadoresHomeAsync(int cajeroId);

    Task<(string? error, RetiroViewModel? confirm)> PreviewRetiroAsync(int cajeroId, RetiroViewModel model);
    Task<OperationResultViewModel> EjecutarRetiroAsync(int cajeroId, RetiroViewModel model);

    Task<(string? error, PagoTarjetaViewModel? confirm)> PreviewPagoTarjetaCajeroAsync(int cajeroId, PagoTarjetaViewModel model);
    Task<OperationResultViewModel> EjecutarPagoTarjetaCajeroAsync(int cajeroId, PagoTarjetaViewModel model);
}
