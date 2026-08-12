using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cajero;
using Microsoft.AspNetCore.Authorization;

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    // [Authorize(Roles = "Cajero")] // Descomentar cuando la seguridad esté lista
    public class CajeroController : Controller
    {
        private readonly ICuentaAhorroService _cuentaAhorroService;
         private readonly ITarjetaCreditoService _tarjetaCreditoService;

        public CajeroController(ICuentaAhorroService cuentaAhorroService, ITarjetaCreditoService tarjetaCreditoService)
        {
            _cuentaAhorroService = cuentaAhorroService;
             _tarjetaCreditoService = tarjetaCreditoService;
        }

        // FUNCIONALIDAD: HOME DEL CAJERO
        [HttpGet]
        public IActionResult Home()
        {
            return View();
        }

        // FUNCIONALIDAD: DEPÓSITO
        [HttpGet]
        public IActionResult Deposito()
        {
            return View(new DepositoViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Deposito(DepositoViewModel vm)
        {
            int cajeroId = 2; // <--- HARDCODED PARA PRUEBAS (Cambiar al ID del usuario actual)

            if (!ModelState.IsValid)
                return View(vm);

            var result = await _cuentaAhorroService.RealizarDepositoAsync(vm, cajeroId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Depósito de RD${vm.Monto} a la cuenta {vm.NumeroCuenta} realizado exitosamente.";
                return RedirectToAction("Deposito");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(vm);
        }

        // RETIRO 
        [HttpGet]
        public IActionResult Retiro() => View(new RetiroViewModel());
        [HttpPost]
        public async Task<IActionResult> Retiro(RetiroViewModel vm)
        {
            int cajeroId = 2; // ID PRUEBA
            if (!ModelState.IsValid) return View(vm);
            var result = await _cuentaAhorroService.RealizarRetiroAsync(vm, cajeroId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Retiro de RD${vm.Monto} realizado exitosamente.";
                return RedirectToAction("Retiro");
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(vm);
        }
        // --- PAGO A TARJETA ---
        [HttpGet]
        public IActionResult PagoTarjeta() => View(new PagoTarjetaViewModel());
        [HttpPost]
        public async Task<IActionResult> PagoTarjeta(PagoTarjetaViewModel vm)
        {
            int cajeroId = 2; // ID PRUEBA
            if (!ModelState.IsValid) return View(vm);
            var result = await _tarjetaCreditoService.RealizarPagoAsync(vm, cajeroId);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Pago de RD${vm.Monto} a la tarjeta realizado exitosamente.";
                return RedirectToAction("PagoTarjeta");
            }
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(vm);
        }
    }
}