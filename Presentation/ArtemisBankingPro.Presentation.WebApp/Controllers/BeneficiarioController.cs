using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Beneficiarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

[Authorize(Roles = "Cliente")]
public class BeneficiarioController : Controller
{
    private readonly IBeneficiarioService _beneficiarioService;

    public BeneficiarioController(IBeneficiarioService beneficiarioService)
    {
        _beneficiarioService = beneficiarioService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && int.TryParse(claim.Value, out int userId))
        {
            return userId;
        }
        // Fallback for testing if claims are different, usually NameIdentifier holds the user ID.
        // Assuming ID is in NameIdentifier
        return int.Parse(claim?.Value ?? "0");
    }

    public async Task<IActionResult> Index()
    {
        var clienteId = GetCurrentUserId();
        if (clienteId == 0) return RedirectToAction("AccessDenied", "Account"); // Fallback
        
        var list = await _beneficiarioService.GetAllByClienteIdAsync(clienteId);
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> AddBeneficiario(SaveBeneficiarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            // Retorna al index con el modelo para mostrar errores en el modal o vista
            var clienteId = GetCurrentUserId();
            var list = await _beneficiarioService.GetAllByClienteIdAsync(clienteId);
            ViewBag.ShowAddModal = true;
            return View("Index", list);
        }

        var error = await _beneficiarioService.AddBeneficiarioAsync(GetCurrentUserId(), vm);
        
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            var clienteId = GetCurrentUserId();
            var list = await _beneficiarioService.GetAllByClienteIdAsync(clienteId);
            ViewBag.ShowAddModal = true;
            return View("Index", list);
        }

        TempData["SuccessMessage"] = "Beneficiario agregado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteBeneficiario(int id)
    {
        var success = await _beneficiarioService.DeleteBeneficiarioAsync(GetCurrentUserId(), id);
        
        if (success)
        {
            TempData["SuccessMessage"] = "Beneficiario eliminado correctamente.";
        }
        else
        {
            TempData["ErrorMessage"] = "Error al eliminar el beneficiario.";
        }
        
        return RedirectToAction(nameof(Index));
    }
}
