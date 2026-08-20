using ArtemisBankingPro.Application.ViewModels.Comercios;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Interfaces.Services;

public interface IComercioService
{
    Task<List<ComercioViewModel>> GetAllComerciosFilteredAsync(string estadoFiltro);
    Task<ComercioDetalleViewModel?> GetComercioDetailsAsync(int id);
    Task<ComercioOperacionResultado> CrearComercioAsync(SaveComercioViewModel model);
    Task<ComercioOperacionResultado> ActualizarComercioAsync(int id, SaveComercioViewModel model);
    Task<ComercioOperacionResultado> CambiarEstadoComercioAsync(int id, bool? estado);
}
