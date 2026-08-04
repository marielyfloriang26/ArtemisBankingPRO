using ArtemisBankingPro.Application.ViewModels.Transacciones;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface ITransaccionClienteService
{
    // Transacción Express
    Task<ExpressFormViewModel> BuildExpressFormAsync(int clienteId);
    Task<(string? error, ExpressConfirmViewModel? confirm)> PreviewExpressAsync(int clienteId, ExpressFormViewModel model);
    Task<OperationResultViewModel> EjecutarExpressAsync(int clienteId, ExpressConfirmViewModel model);

    // Pago a tarjeta de crédito
    Task<PagoTarjetaFormViewModel> BuildPagoTarjetaFormAsync(int clienteId);
    Task<(string? error, PagoTarjetaConfirmViewModel? confirm)> PreviewPagoTarjetaAsync(int clienteId, PagoTarjetaFormViewModel model);
    Task<OperationResultViewModel> EjecutarPagoTarjetaAsync(int clienteId, PagoTarjetaConfirmViewModel model);

    // Pago a préstamo
    Task<PagoPrestamoFormViewModel> BuildPagoPrestamoFormAsync(int clienteId);
    Task<(string? error, PagoPrestamoConfirmViewModel? confirm)> PreviewPagoPrestamoAsync(int clienteId, PagoPrestamoFormViewModel model);
    Task<OperationResultViewModel> EjecutarPagoPrestamoAsync(int clienteId, PagoPrestamoConfirmViewModel model);

    // Transacción a beneficiarios
    Task<PagoBeneficiarioFormViewModel> BuildPagoBeneficiarioFormAsync(int clienteId);
    Task<(string? error, PagoBeneficiarioConfirmViewModel? confirm)> PreviewPagoBeneficiarioAsync(int clienteId, PagoBeneficiarioFormViewModel model);
    Task<OperationResultViewModel> EjecutarPagoBeneficiarioAsync(int clienteId, PagoBeneficiarioConfirmViewModel model);
}
