using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.AdminTarjeta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

[Authorize(Roles = "Administrador")]
public class AdminTarjetaCreditoController : Controller
{
    private readonly ITarjetaCreditoService _tarjetaService;

    public AdminTarjetaCreditoController(ITarjetaCreditoService tarjetaService)
    {
        _tarjetaService = tarjetaService;
    }

    public async Task<IActionResult> Index(string? cedula, string? estado, int page = 1)
    {
        bool estadoWasNull = string.IsNullOrEmpty(estado);
        if (estadoWasNull && string.IsNullOrEmpty(cedula))
            estado = "Activas";

        var tarjetas = await _tarjetaService.GetAllTarjetasFilteredAsync(cedula, estadoWasNull ? "Todas" : estado);

        if (!string.IsNullOrEmpty(cedula))
        {
            if (!await _tarjetaService.ExisteClienteConCedulaAsync(cedula))
            {
                ViewBag.ErrorMessage = "No existe un cliente registrado con esta cédula.";
                tarjetas = new List<TarjetaAdminViewModel>();
            }
            else if (!tarjetas.Any())
            {
                ViewBag.ErrorMessage = "Este cliente no tiene tarjetas de crédito registradas.";
            }
        }

        int pageSize = 20;
        int total = tarjetas.Count;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Cedula = cedula;
        ViewBag.Estado = estadoWasNull ? "Todas" : estado;

        tarjetas = tarjetas.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return View(tarjetas);
    }

    public async Task<IActionResult> SelectClient(string? cedula)
    {
        var promedio = await _tarjetaService.CalcularDeudaPromedioGlobalAsync();
        ViewBag.DeudaPromedio = promedio;
        ViewBag.Cedula = cedula;

        var clientes = await _tarjetaService.GetClientesElegiblesAsync(cedula);
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

        var elegibles = await _tarjetaService.GetClientesElegiblesAsync(null);
        var cliente = elegibles.FirstOrDefault(c => c.Id == clienteId.Value);
        if (cliente == null)
        {
            TempData["Error"] = "Solo se puede asignar tarjetas de crédito a clientes activos.";
            return RedirectToAction(nameof(SelectClient));
        }

        return RedirectToAction(nameof(Create), new { clienteId = clienteId.Value });
    }

    public IActionResult Create(int clienteId)
    {
        var model = new AssignTarjetaViewModel { ClienteId = clienteId };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AssignTarjetaViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(adminIdStr, out int adminId))
        {
            model.AdminId = adminId;
        }

        string result = await _tarjetaService.AsignarTarjetaAsync(model);
        if (!string.IsNullOrEmpty(result) && !result.Contains("no fue posible enviar el correo"))
        {
            TempData["Error"] = result;
            return View(model);
        }
        
        if (!string.IsNullOrEmpty(result) && result.Contains("no fue posible enviar el correo"))
        {
            TempData["Warning"] = result;
        }
        else
        {
            TempData["Success"] = "Tarjeta asignada correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var consumos = await _tarjetaService.GetConsumosTarjetaAsync(id);
        ViewBag.TarjetaId = id;
        return View(consumos);
    }

    public async Task<IActionResult> EditLimit(int id)
    {
        var model = await _tarjetaService.GetEditLimiteViewModelAsync(id);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditLimit(EditLimiteTarjetaViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        string result = await _tarjetaService.EditLimiteAsync(model);
        if (!string.IsNullOrEmpty(result) && !result.Contains("no fue posible enviar el correo"))
        {
            TempData["Error"] = result;
            return View(model);
        }

        if (!string.IsNullOrEmpty(result) && result.Contains("no fue posible enviar el correo"))
        {
            TempData["Warning"] = result;
        }
        else
        {
            TempData["Success"] = "Límite actualizado correctamente.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        string result = await _tarjetaService.CancelTarjetaAsync(id);
        if (!string.IsNullOrEmpty(result))
        {
            TempData["Error"] = result;
        }
        else
        {
            TempData["Success"] = "Tarjeta cancelada correctamente.";
        }
        return RedirectToAction(nameof(Index));
    }
}
