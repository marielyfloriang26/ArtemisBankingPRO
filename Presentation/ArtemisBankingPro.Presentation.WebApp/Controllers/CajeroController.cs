using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cajero;
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
    private readonly ICuentaAhorroService _cuentaAhorroService;
    private readonly ITarjetaCreditoService _tarjetaCreditoService;

    public CajeroController(
        ITransaccionCajeroService transaccionCajeroService,
        ICuentaAhorroService cuentaAhorroService,
        ITarjetaCreditoService tarjetaCreditoService)
    {
        _transaccionCajeroService = transaccionCajeroService;
        _cuentaAhorroService = cuentaAhorroService;
        _tarjetaCreditoService = tarjetaCreditoService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out int userId) ? userId : 0;
    }

    #region Home

    [HttpGet]
    public async Task<IActionResult> Home()
    {
        var (transacciones, pagos, depositos, retiros) = await _transaccionCajeroService.GetIndicadoresHomeAsync(GetCurrentUserId());

        ViewBag.TransaccionesHoy = transacciones;
        ViewBag.PagosHoy = pagos;
        ViewBag.DepositosHoy = depositos;
        ViewBag.RetirosHoy = retiros;

        return View();
    }

    #endregion

    #region Depósito

    [HttpGet]
    public IActionResult Deposito()
    {
        return View(new DepositoViewModel());
    }

    [HttpPost]
    [HttpPost]
public async Task<IActionResult> Deposito(DepositoViewModel vm)
{
    if (!ModelState.IsValid) return View(vm);

    var (error, confirm) = await _transaccionCajeroService.PreviewDepositoAsync(GetCurrentUserId(), vm);
    if (error != null)
    {
        TempData["ErrorMessage"] = error;
        return View(vm);
    }
    return View("DepositoConfirm", confirm);
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DepositoConfirm(DepositoViewModel model)
{
    var result = await _transaccionCajeroService.EjecutarDepositoAsync(GetCurrentUserId(), model);
    TempData[result.Success ? (result.CorreoFallido ? "WarningMessage" : "SuccessMessage") : "ErrorMessage"] = result.Message;
    return RedirectToAction("Home");
}

    #endregion

    #region Retiro

    [HttpGet]
    public IActionResult Retiro() => View(new RetiroViewModel());

    [HttpPost]
    public async Task<IActionResult> Retiro(RetiroViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (error, confirm) = await _transaccionCajeroService.PreviewRetiroAsync(GetCurrentUserId(), vm);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return View(vm);
        }

        return View("RetiroConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetiroConfirm(RetiroViewModel model)
    {
        var result = await _transaccionCajeroService.EjecutarRetiroAsync(GetCurrentUserId(), model);
        TempData[result.Success ? (result.CorreoFallido ? "WarningMessage" : "SuccessMessage") : "ErrorMessage"] = result.Message;
        return RedirectToAction("Home");
    }

    #endregion

    #region Pago a tarjeta de crédito

    [HttpGet]
    public IActionResult PagoTarjeta() => View(new PagoTarjetaViewModel());

    [HttpPost]
    public async Task<IActionResult> PagoTarjeta(PagoTarjetaViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (error, confirm) = await _transaccionCajeroService.PreviewPagoTarjetaCajeroAsync(GetCurrentUserId(), vm);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return View(vm);
        }

        return View("PagoTarjetaConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PagoTarjetaConfirm(PagoTarjetaViewModel model)
    {
        var result = await _transaccionCajeroService.EjecutarPagoTarjetaCajeroAsync(GetCurrentUserId(), model);
        TempData[result.Success ? (result.CorreoFallido ? "WarningMessage" : "SuccessMessage") : "ErrorMessage"] = result.Message;
        return RedirectToAction("Home");
    }

    #endregion

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

    #region Transacciones a cuentas de terceros

    public IActionResult TransaccionTerceros()
    {
        return View(new TransaccionTercerosCajeroFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransaccionTerceros(TransaccionTercerosCajeroFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var cajeroId = GetCurrentUserId();
        var (error, confirm) = await _transaccionCajeroService.PreviewTransaccionTercerosAsync(cajeroId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            return View(model);
        }

        return View("TransaccionTercerosConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransaccionTercerosConfirm(TransaccionTercerosCajeroConfirmViewModel model)
    {
        var result = await _transaccionCajeroService.EjecutarTransaccionTercerosAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(TransaccionTerceros));
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
