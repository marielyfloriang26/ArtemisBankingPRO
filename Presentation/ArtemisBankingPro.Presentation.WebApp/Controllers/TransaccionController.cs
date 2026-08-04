using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Transacciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers;

[Authorize(Roles = "Cliente")]
public class TransaccionController : Controller
{
    private readonly ITransaccionClienteService _transaccionService;

    public TransaccionController(ITransaccionClienteService transaccionService)
    {
        _transaccionService = transaccionService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out int userId) ? userId : 0;
    }

    public IActionResult Index()
    {
        return View();
    }

    #region Transacción Express

    public async Task<IActionResult> Express()
    {
        var model = await _transaccionService.BuildExpressFormAsync(GetCurrentUserId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Express(ExpressFormViewModel model)
    {
        var clienteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            model.CuentasOrigen = (await _transaccionService.BuildExpressFormAsync(clienteId)).CuentasOrigen;
            return View(model);
        }

        var (error, confirm) = await _transaccionService.PreviewExpressAsync(clienteId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            model.CuentasOrigen = (await _transaccionService.BuildExpressFormAsync(clienteId)).CuentasOrigen;
            return View(model);
        }

        return View("ExpressConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExpressConfirm(ExpressConfirmViewModel model)
    {
        var result = await _transaccionService.EjecutarExpressAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Express));
        }

        if (result.CorreoFallido)
        {
            TempData["WarningMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }
        return RedirectToAction("Cliente", "Home");
    }

    #endregion

    #region Pago a tarjeta de crédito

    public async Task<IActionResult> Tarjeta()
    {
        var model = await _transaccionService.BuildPagoTarjetaFormAsync(GetCurrentUserId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Tarjeta(PagoTarjetaFormViewModel model)
    {
        var clienteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var rebuilt = await _transaccionService.BuildPagoTarjetaFormAsync(clienteId);
            model.Tarjetas = rebuilt.Tarjetas;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        var (error, confirm) = await _transaccionService.PreviewPagoTarjetaAsync(clienteId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            var rebuilt = await _transaccionService.BuildPagoTarjetaFormAsync(clienteId);
            model.Tarjetas = rebuilt.Tarjetas;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        return View("TarjetaConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TarjetaConfirm(PagoTarjetaConfirmViewModel model)
    {
        var result = await _transaccionService.EjecutarPagoTarjetaAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Tarjeta));
        }

        if (result.CorreoFallido)
        {
            TempData["WarningMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }
        return RedirectToAction("Cliente", "Home");
    }

    #endregion

    #region Pago a préstamo

    public async Task<IActionResult> Prestamo()
    {
        var model = await _transaccionService.BuildPagoPrestamoFormAsync(GetCurrentUserId());
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Prestamo(PagoPrestamoFormViewModel model)
    {
        var clienteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var rebuilt = await _transaccionService.BuildPagoPrestamoFormAsync(clienteId);
            model.Prestamos = rebuilt.Prestamos;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        var (error, confirm) = await _transaccionService.PreviewPagoPrestamoAsync(clienteId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            var rebuilt = await _transaccionService.BuildPagoPrestamoFormAsync(clienteId);
            model.Prestamos = rebuilt.Prestamos;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        return View("PrestamoConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PrestamoConfirm(PagoPrestamoConfirmViewModel model)
    {
        var result = await _transaccionService.EjecutarPagoPrestamoAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Prestamo));
        }

        if (result.CorreoFallido)
        {
            TempData["WarningMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }
        return RedirectToAction("Cliente", "Home");
    }

    #endregion

    #region Transacción a beneficiarios

    public async Task<IActionResult> Beneficiario()
    {
        var model = await _transaccionService.BuildPagoBeneficiarioFormAsync(GetCurrentUserId());
        if (!model.Beneficiarios.Any())
        {
            TempData["ErrorMessage"] = "No tiene beneficiarios registrados.";
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Beneficiario(PagoBeneficiarioFormViewModel model)
    {
        var clienteId = GetCurrentUserId();

        if (!ModelState.IsValid)
        {
            var rebuilt = await _transaccionService.BuildPagoBeneficiarioFormAsync(clienteId);
            model.Beneficiarios = rebuilt.Beneficiarios;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        var (error, confirm) = await _transaccionService.PreviewPagoBeneficiarioAsync(clienteId, model);
        if (error != null)
        {
            TempData["ErrorMessage"] = error;
            var rebuilt = await _transaccionService.BuildPagoBeneficiarioFormAsync(clienteId);
            model.Beneficiarios = rebuilt.Beneficiarios;
            model.CuentasOrigen = rebuilt.CuentasOrigen;
            return View(model);
        }

        return View("BeneficiarioConfirm", confirm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BeneficiarioConfirm(PagoBeneficiarioConfirmViewModel model)
    {
        var result = await _transaccionService.EjecutarPagoBeneficiarioAsync(GetCurrentUserId(), model);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Beneficiario));
        }

        if (result.CorreoFallido)
        {
            TempData["WarningMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }
        return RedirectToAction("Cliente", "Home");
    }

    #endregion
}
