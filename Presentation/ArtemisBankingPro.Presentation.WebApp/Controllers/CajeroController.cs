using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Transacciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

[Authorize(Roles = "Cajero")]
public class CajeroController : Controller
{
    private readonly ITransaccionCajeroService _transaccionCajeroService;

    public CajeroController(ITransaccionCajeroService transaccionCajeroService)
    {
        _transaccionCajeroService = transaccionCajeroService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out int userId) ? userId : 0;
    }

    #region Pago a préstamo

    public IActionResult PagoPrestamo()
    {
        return View(new PagoPrestamoCajeroFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoPrestamo(PagoPrestamoCajeroFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cajeroId = GetCurrentUserId();
        var (error, confirm) = await _transaccionCajeroService.PreviewPagoPrestamoAsync(cajeroId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return View(model);
        }

        return View("PagoPrestamoConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoPrestamoConfirm(PagoPrestamoCajeroConfirmViewModel model)
    {
        var result = await _transaccionCajeroService.EjecutarPagoPrestamoAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(PagoPrestamo));
        }

        if (result.CorreoFallido)
        {
            TempData["WarningMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }
        return RedirectToAction("Cajero", "Home");
    }

    #endregion
}
