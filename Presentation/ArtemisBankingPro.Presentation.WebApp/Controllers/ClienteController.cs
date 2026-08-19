using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cliente;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims; // Para obtener el ID del usuario

namespace ArtemisBankingPro.Presentation.WebApp.Controllers
{
    // [Authorize(Roles = "Cliente")] // Descomentar esto cuando tengas Identity configurado
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

        // MÉTODO HOME / LISTADO DE PRODUCTOS
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

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
            // TODO: Ajustar según cómo obtengas el ID del usuario logueado. 
            // int clienteId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            int clienteId = 1; // <--- HARDCODED PARA PRUEBAS (Cámbialo luego)

            var vm = new AvanceEfectivoViewModel();
            ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> AvanceEfectivo(AvanceEfectivoViewModel vm)
        {
            int clienteId = 1; // <--- HARDCODED PARA PRUEBAS (Cámbialo luego)

            if (!ModelState.IsValid)
            {
                ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
                ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
                return View(vm);
            }

            var result = await _tarjetaCreditoService.RealizarAvanceEfectivoAsync(vm, clienteId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Avance de efectivo realizado con éxito.";
                return RedirectToAction("AvanceEfectivo"); // O redirigir al Home del cliente
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewBag.Tarjetas = await _tarjetaCreditoService.GetActiveCardsByClientIdAsync(clienteId);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

                [HttpGet]
        public async Task<IActionResult> Transferencia()
        {
            int clienteId = 1; // <--- HARDCODED PARA PRUEBAS

            var vm = new TransferenciaViewModel();
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Transferencia(TransferenciaViewModel vm)
        {
            int clienteId = 1; // <--- HARDCODED PARA PRUEBAS

            if (!ModelState.IsValid)
            {
                ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
                return View(vm);
            }

            var result = await _cuentaAhorroService.RealizarTransferenciaAsync(vm, clienteId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Transferencia realizada con éxito.";
                return RedirectToAction("Transferencia");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            ViewBag.Cuentas = await _cuentaAhorroService.GetActiveCuentasByClientIdAsync(clienteId);
            return View(vm);
        }
    }
}