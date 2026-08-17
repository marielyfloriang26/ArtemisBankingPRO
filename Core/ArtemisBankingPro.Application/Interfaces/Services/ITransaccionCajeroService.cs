using ArtemisBankingPro.Application.ViewModels.Transacciones;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface ITransaccionCajeroService
{
    // Pago a préstamo
    Task<(string? error, PagoPrestamoCajeroConfirmViewModel? confirm)> PreviewPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroFormViewModel model);
    Task<OperationResultViewModel> EjecutarPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroConfirmViewModel model);
}
