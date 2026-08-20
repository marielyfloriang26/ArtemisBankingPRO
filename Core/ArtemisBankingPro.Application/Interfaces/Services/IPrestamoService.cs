using ArtemisBankingPro.Application.ViewModels.Prestamos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IPrestamoService
{
    Task<List<PrestamoViewModel>> GetAllPrestamosFilteredAsync(string? cedula, string estadoFiltro);
    Task<List<ClienteElegibleViewModel>> GetClientesElegiblesAsync(string? cedula);
    Task<decimal> CalcularDeudaPromedioGlobalAsync();
    Task<decimal> CalcularDeudaTotalClienteAsync(int clienteId);
    Task<decimal> CalcularDeudaProyectadaAsync(int clienteId, decimal monto, decimal tasaAnual, int plazo);
    Task<string> AsignarPrestamoAsync(SavePrestamoViewModel model);
    Task<PrestamoViewModel?> GetPrestamoDetailsAsync(int id);
    Task<EditTasaPrestamoViewModel?> GetEditTasaViewModelAsync(int id);
    Task<string> EditTasaInteresAsync(EditTasaPrestamoViewModel model);
    Task<bool> ExisteClienteConCedulaAsync(string cedula);
    Task<bool> ExisteClientePorIdAsync(int clienteId);
    Task<bool> ExistePrestamoAsync(int id);
    Task<string> ValidarElegibilidadPrestamoAsync(int clienteId);
    Task<RiesgoPrestamoResultado> EvaluarRiesgoAsync(int clienteId, decimal monto, decimal tasaAnual, int plazo);
    Task<PrestamoViewModel?> GetPrestamoActivoByClienteIdAsync(int clienteId);
    Task<List<dynamic>> GetTablaAmortizacionByPrestamoIdAsync(int prestamoId, int clienteId);
}
