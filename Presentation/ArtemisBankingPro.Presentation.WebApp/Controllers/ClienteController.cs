using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Para obtener el ID del usuario

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    [Authorize(Roles = "Cliente")]
    public class ClienteController : Controller
    {
        private readonly ITarjetaCreditoService _tarjetaCreditoService;
        private readonly ICuentaAhorroService _cuentaAhorroService;
        private readonly IPrestamoService _prestamoService;

        public ClienteController(ITarjetaCreditoService tarjetaCreditoService, ICuentaAhorroService cuentaAhorroService, IPrestamoService prestamoService)
        {
            _tarjetaCreditoService = tarjetaCreditoService;
            _cuentaAhorroService = cuentaAhorroService;
            _prestamoService = prestamoService;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out int userId) ? userId : 0;
        }

        // MÉTODO HOME / LISTADO DE PRODUCTOS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int clienteId = GetCurrentUserId();

            var cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            // Filtramos los préstamos del cliente que estén activos/al día usando el método general de filtrado o ajustando el servicio
            var todosLosPrestamos = await _prestamoService.GetAllPrestamosFilteredAsync(null, "Activos");
            var prestamos = todosLosPrestamos.Where(p => p.ClienteId == clienteId).ToList();

            var tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);

            // Regla de ordenamiento: Cuenta Principal primero, luego secundarias de mayor a menor balance
            var cuentasOrdenadas = cuentas
                .OrderByDescending(c => c.TipoCuenta == "Principal") 
                .ThenByDescending(c => c.Balance)
                .ToList();

            ViewBag.Cuentas = cuentasOrdenadas;
            ViewBag.Prestamos = prestamos;
            ViewBag.Tarjetas = tarjetas;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> AvanceEfectivo()
        {
            int clienteId = GetCurrentUserId();

            var vm = new AvanceEfectivoViewModel();
            ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AvanceEfectivo(AvanceEfectivoViewModel vm)
        {
            int clienteId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
                ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
                return View(vm);
            }

            var result = await _tarjetaCreditoService.RealizarAvanceEfectivoAsync(vm, clienteId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = string.IsNullOrEmpty(result.ErrorMessage) ? "Avance de efectivo realizado con éxito." : result.ErrorMessage; // mensaje warning si fallo el correo
                return RedirectToAction("Index"); // redirigir al Home del cliente
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Transferencia()
        {
            int clienteId = GetCurrentUserId();

            var vm = new TransferenciaViewModel();
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Transferencia(TransferenciaViewModel vm)
        {
            int clienteId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
                return View(vm);
            }

            var cuentasCliente = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            var cuentaOrigen = cuentasCliente.FirstOrDefault(c => c.Id == vm.CuentaOrigenId);
            ViewBag.NumeroCuentaOrigen = cuentaOrigen?.NumeroCuenta;
            
            return View("TransferenciaConfirmacion", vm);
        }

        [HttpPost]
        public async Task<IActionResult> TransferenciaConfirmar(TransferenciaViewModel vm)
        {
            int clienteId = GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
                return View("Transferencia", vm);
            }

            var result = await _cuentaAhorroService.RealizarTransferenciaAsync(vm, clienteId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = string.IsNullOrEmpty(result.ErrorMessage) ? "Transferencia realizada con éxito." : result.ErrorMessage; // mensaje warning si fallo el correo
                return RedirectToAction("Index"); // redirigir al Home del cliente
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            return View("Transferencia", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DetallesCuenta(int id)
        {
            int clienteId = GetCurrentUserId();
            var transacciones = await _cuentaAhorroService.GetTransaccionesByCuentaIdAsync(id, clienteId);
            
            if (transacciones == null)
            {
                return NotFound();
            }

            return View(transacciones);
        }

        [HttpGet]
        public async Task<IActionResult> DetallesPrestamo(int id)
        {
            int clienteId = GetCurrentUserId();
            var amortizacion = await _prestamoService.GetTablaAmortizacionByPrestamoIdAsync(id, clienteId);
            
            if (amortizacion == null)
            {
                return NotFound();
            }

            return View(amortizacion);
        }

        [HttpGet]
        public async Task<IActionResult> DetallesTarjeta(int id)
        {
            int clienteId = GetCurrentUserId();
            var consumos = await _tarjetaCreditoService.GetConsumosByTarjetaIdAsync(id, clienteId);
            
            if (consumos == null)
            {
                return NotFound();
            }

            return View(consumos);
        }

    }
}