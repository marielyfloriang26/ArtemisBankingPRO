using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Prestamos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

[Authorize(Roles = "Administrador")]
public class PrestamoController : Controller
{
    private readonly IPrestamoService _prestamoService;

    public PrestamoController(IPrestamoService prestamoService)
    {
        _prestamoService = prestamoService;
    }

    public async Task<IActionResult> Index(string? cedula, string? estado, int page = 1)
    {
        bool estadoWasNull = string.IsNullOrEmpty(estado);
        if (estadoWasNull && string.IsNullOrEmpty(cedula))
            estado = "Activos";

        var prestamos = await _prestamoService.GetAllPrestamosFilteredAsync(cedula, estadoWasNull ? "Todos" : estado);

        if (!string.IsNullOrEmpty(cedula))
        {
            if (!await _prestamoService.ExisteClienteConCedulaAsync(cedula))
            {
                ViewBag.ErrorMessage = "No existe un cliente registrado con esta cédula.";
                prestamos = new List<PrestamoViewModel>();
            }
            else if (!prestamos.Any())
            {
                ViewBag.ErrorMessage = "Este cliente no tiene préstamos registrados.";
            }
        }

        int pageSize = 20;
        int total = prestamos.Count;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Cedula = cedula;
        ViewBag.Estado = estadoWasNull ? "Todos" : estado;

        prestamos = prestamos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return View(prestamos);
    }

    public async Task<IActionResult> SelectClient(string? cedula)
    {
        var promedio = await _prestamoService.CalcularDeudaPromedioGlobalAsync();
        ViewBag.DeudaPromedio = promedio;
        ViewBag.Cedula = cedula;

        var clientes = await _prestamoService.GetClientesElegiblesAsync(cedula);
        return View(clientes);
    }

    [HttpPost]
    public async Task<IActionResult> SelectClientPost(int? clienteId)
    {
        if (clienteId == null)
        {
            TempData["Error"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(SelectClient));
        }

        var elegibles = await _prestamoService.GetClientesElegiblesAsync(null);
        if (!elegibles.Any(c => c.Id == clienteId.Value))
        {
            TempData["Error"] = "Este cliente ya tiene un préstamo activo asignado.";
            return RedirectToAction(nameof(SelectClient));
        }

        return RedirectToAction(nameof(Create), new { clienteId = clienteId.Value });
    }

    public IActionResult Create(int clienteId)
    {
        var model = new SavePrestamoViewModel { ClienteId = clienteId };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SavePrestamoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        decimal promedio = await _prestamoService.CalcularDeudaPromedioGlobalAsync();
        decimal actual = await _prestamoService.CalcularDeudaTotalClienteAsync(model.ClienteId);
        decimal proyectada = await _prestamoService.CalcularDeudaProyectadaAsync(model.ClienteId, model.MontoAprobado, model.TasaInteresAnual, model.PlazoMeses);

        if (actual > promedio)
        {
            TempData["RiskWarning"] = "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema.";
            TempData["Model"] = System.Text.Json.JsonSerializer.Serialize(model);
            return RedirectToAction(nameof(RiskWarning));
        }
        else if (proyectada > promedio)
        {
            TempData["RiskWarning"] = "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.";
            TempData["Model"] = System.Text.Json.JsonSerializer.Serialize(model);
            return RedirectToAction(nameof(RiskWarning));
        }

        return await ProcessCreate(model);
    }

    public IActionResult RiskWarning()
    {
        if (TempData["RiskWarning"] == null || TempData["Model"] == null)
            return RedirectToAction(nameof(Index));

        ViewBag.Warning = TempData["RiskWarning"];
        var modelJson = TempData["Model"]?.ToString();
        var model = System.Text.Json.JsonSerializer.Deserialize<SavePrestamoViewModel>(modelJson!);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmRiskWarning(SavePrestamoViewModel model)
    {
        return await ProcessCreate(model);
    }

    private async Task<IActionResult> ProcessCreate(SavePrestamoViewModel model)
    {
        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(adminIdStr, out int adminId))
        {
            model.AdminId = adminId;
        }

        string result = await _prestamoService.AsignarPrestamoAsync(model);
        if (!string.IsNullOrEmpty(result) && !result.Contains("no fue posible enviar el correo"))
        {
            TempData["Error"] = result;
            return View("Create", model);
        }
        
        if (!string.IsNullOrEmpty(result) && result.Contains("no fue posible enviar el correo"))
        {
            TempData["Warning"] = result;
        }
        else
        {
            TempData["Success"] = "Préstamo asignado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var prestamo = await _prestamoService.GetPrestamoDetailsAsync(id);
        if (prestamo == null) return NotFound();

        return View(prestamo);
    }

    public async Task<IActionResult> EditRate(int id)
    {
        var model = await _prestamoService.GetEditTasaViewModelAsync(id);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditRate(EditTasaPrestamoViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string result = await _prestamoService.EditTasaInteresAsync(model);
        if (!string.IsNullOrEmpty(result))
        {
            TempData["Error"] = result;
            return View(model);
        }

        TempData["Success"] = "Tasa de interés actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id = model.PrestamoId });
    }
}
