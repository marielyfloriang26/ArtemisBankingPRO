using ArtemisBankingPro.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using ArtemisBankingPro.Application.ViewModels.CuentaAhorro;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

[Authorize(Roles = "Administrador")]
public class AdminCuentaAhorroController : Controller
{
    private readonly UserManager<Usuario> _userManager;
    private readonly ApplicationDbContext _context;

    public AdminCuentaAhorroController(UserManager<Usuario> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // GET: AdminCuentaAhorro/Index
    public async Task<IActionResult> Index(string cedulaFiltro, string estadoFiltro, string tipoFiltro, int page = 1)
    {
        int pageSize = 20;

        var query = _context.CuentasAhorro
            .Include(c => c.Cliente)
            .AsQueryable();

        // Filtro por Cédula
        if (!string.IsNullOrWhiteSpace(cedulaFiltro))
        {
            var cliente = await _userManager.Users.FirstOrDefaultAsync(u => u.Cedula == cedulaFiltro.Trim());
            if (cliente == null)
            {
                TempData["Error"] = "No existe un cliente registrado con esta cédula.";
                return View(new List<CuentaAhorro>());
            }

            query = query.Where(c => c.ClienteId == cliente.Id);

            var tieneCuentas = await query.AnyAsync();
            if (!tieneCuentas)
            {
                TempData["Error"] = "Este cliente no tiene cuentas de ahorro registradas.";
                return View(new List<CuentaAhorro>());
            }

            // Si busca por cédula y no especifica estado, muestra activas primero, luego canceladas, más recientes a más antiguas
            if (string.IsNullOrEmpty(estadoFiltro) || estadoFiltro == "Todas")
            {
                query = query.OrderByDescending(c => c.Estado == "Activa")
                             .ThenByDescending(c => c.FechaCreacion);
            }
        }

        // Filtro por Estado
        if (!string.IsNullOrEmpty(estadoFiltro))
        {
            if (estadoFiltro == "Activas")
                query = query.Where(c => c.Estado == "Activa");
            else if (estadoFiltro == "Canceladas")
                query = query.Where(c => c.Estado == "Cancelada");
        }
        else if (string.IsNullOrWhiteSpace(cedulaFiltro))
        {
            // Por defecto, mostrar cuentas activas
            query = query.Where(c => c.Estado == "Activa");
        }

        // Filtro por Tipo
        if (!string.IsNullOrEmpty(tipoFiltro) && tipoFiltro != "Todas")
        {
            query = query.Where(c => c.TipoCuenta == tipoFiltro);
        }

        // Ordenamiento por defecto si no se aplicó el ordenamiento especial de cédula
        if (string.IsNullOrWhiteSpace(cedulaFiltro) || (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todas"))
        {
            query = query.OrderByDescending(c => c.FechaCreacion);
        }

        int totalCuentas = await query.CountAsync();
        var cuentas = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CedulaFiltro = cedulaFiltro;
        ViewBag.EstadoFiltro = estadoFiltro ?? "Activas";
        ViewBag.TipoFiltro = tipoFiltro ?? "Todas";
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCuentas / (double)pageSize);

        return View(cuentas);
    }

    // GET: AdminCuentaAhorro/SelectClient
    public async Task<IActionResult> SelectClient(string cedulaBusqueda, int page = 1)
    {
        int pageSize = 15;
        var query = _userManager.Users
            .Where(u => u.TipoUsuario == "Cliente" && u.EsActivo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(cedulaBusqueda))
        {
            query = query.Where(u => u.Cedula.Contains(cedulaBusqueda.Trim()));
        }

        var clientesRaw = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var clientesViewModel = new List<ClientDebtViewModel>();
        foreach (var client in clientesRaw)
        {
            decimal deudaPrestamos = await _context.Prestamos
                .Where(p => p.ClienteId == client.Id && p.Estado != "Completado")
                .SumAsync(p => p.MontoPendiente);

            decimal deudaTarjetas = await _context.TarjetasCredito
                .Where(t => t.ClienteId == client.Id && t.Estado == "Activa")
                .SumAsync(t => t.MontoAdeudado);

            clientesViewModel.Add(new ClientDebtViewModel
            {
                Cliente = client,
                MontoTotalDeuda = deudaPrestamos + deudaTarjetas
            });
        }

        ViewBag.CedulaBusqueda = cedulaBusqueda;
        return View(clientesViewModel);
    }

    // GET: AdminCuentaAhorro/CreateSecondary/5
    public async Task<IActionResult> CreateSecondary(int? clientId)
    {
        if (clientId == null || clientId == 0)
        {
            TempData["Error"] = "Debe seleccionar un cliente para continuar.";
            return RedirectToAction(nameof(SelectClient));
        }

        var cliente = await _userManager.FindByIdAsync(clientId.ToString());
        if (cliente == null)
        {
            TempData["Error"] = "El cliente seleccionado no existe.";
            return RedirectToAction(nameof(SelectClient));
        }
        if (!cliente.EsActivo)
        {
            TempData["Error"] = "Solo se puede asignar cuentas de ahorro a clientes activos.";
            return RedirectToAction(nameof(SelectClient));
        }

        bool tienePrincipalActiva = await _context.CuentasAhorro
            .AnyAsync(c => c.ClienteId == clientId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (!tienePrincipalActiva)
        {
            TempData["Error"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
            return RedirectToAction(nameof(SelectClient));
        }

        ViewBag.Cliente = cliente;
        return View();
    }

    // POST: AdminCuentaAhorro/CreateSecondary
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSecondary(int clienteId, decimal? balanceInicial)
    {
        var cliente = await _userManager.FindByIdAsync(clienteId.ToString());
        if (cliente == null)
        {
            TempData["Error"] = "El cliente seleccionado no existe.";
            return RedirectToAction(nameof(SelectClient));
        }
        if (!cliente.EsActivo)
        {
            TempData["Error"] = "Solo se puede asignar cuentas de ahorro a clientes activos.";
            return RedirectToAction(nameof(SelectClient));
        }

        bool tienePrincipalActiva = await _context.CuentasAhorro
            .AnyAsync(c => c.ClienteId == clienteId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (!tienePrincipalActiva)
        {
            TempData["Error"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
            return RedirectToAction(nameof(SelectClient));
        }

        decimal saldoInicial = balanceInicial ?? 0.00m;
        if (saldoInicial < 0)
        {
            TempData["Error"] = "El balance inicial no puede ser negativo.";
            return RedirectToAction(nameof(CreateSecondary), new { clientId = clienteId });
        }

        string numeroCuenta;
        Random rnd = new Random();
        do
        {
            numeroCuenta = rnd.Next(100000000, 999999999).ToString();
        } while (await _context.CuentasAhorro.AnyAsync(c => c.NumeroCuenta == numeroCuenta) ||
                 await _context.Prestamos.AnyAsync(p => p.Id.ToString() == numeroCuenta));

        var adminId = int.Parse(_userManager.GetUserId(User));

        var nuevaCuenta = new CuentaAhorro
        {
            NumeroCuenta = numeroCuenta,
            ClienteId = clienteId,
            Balance = saldoInicial,
            TipoCuenta = "Secundaria",
            Estado = "Activa",
            FechaCreacion = DateTime.UtcNow
        };

        _context.CuentasAhorro.Add(nuevaCuenta);
        await _context.SaveChangesAsync();

        if (saldoInicial > 0)
        {
            var transaccion = new Transaccion
            {
                CuentaDestinoId = nuevaCuenta.Id,
                Monto = saldoInicial,
                TipoTransaccion = "CRÉDITO",
                Origen = "APERTURA",
                Beneficiario = numeroCuenta,
                Estado = "APROBADA",
                UsuarioResponsableId = adminId,
                FechaTransaccion = DateTime.UtcNow
            };
            _context.Transacciones.Add(transaccion);
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Cuenta de ahorro secundaria asignada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    // GET: AdminCuentaAhorro/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var cuenta = await _context.CuentasAhorro
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cuenta == null)
        {
            TempData["Error"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(Index));
        }

        var transacciones = await _context.Transacciones
            .Where(t => t.CuentaOrigenId == id || t.CuentaDestinoId == id)
            .OrderByDescending(t => t.FechaTransaccion)
            .ToListAsync();

        ViewBag.Cuenta = cuenta;
        return View(transacciones);
    }

    // GET: AdminCuentaAhorro/Cancel/5
    public async Task<IActionResult> Cancel(int id)
    {
        var cuenta = await _context.CuentasAhorro
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cuenta == null)
        {
            TempData["Error"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (cuenta.Estado == "Cancelada")
        {
            TempData["Error"] = "La cuenta seleccionada ya se encuentra cancelada.";
            return RedirectToAction(nameof(Index));
        }

        if (cuenta.TipoCuenta == "Principal")
        {
            TempData["Error"] = "Las cuentas principales no pueden ser canceladas.";
            return RedirectToAction(nameof(Index));
        }

        var cuentaPrincipal = await _context.CuentasAhorro
            .FirstOrDefaultAsync(c => c.ClienteId == cuenta.ClienteId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (cuentaPrincipal == null)
        {
            TempData["Error"] = "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.";
            return RedirectToAction(nameof(Index));
        }

        return View(cuenta);
    }

    // POST: AdminCuentaAhorro/CancelConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelConfirmed(int id)
    {
        var cuenta = await _context.CuentasAhorro.FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta == null)
        {
            TempData["Error"] = "La cuenta seleccionada no existe.";
            return RedirectToAction(nameof(Index));
        }

        if (cuenta.Estado == "Cancelada")
        {
            TempData["Error"] = "La cuenta seleccionada ya se encuentra cancelada.";
            return RedirectToAction(nameof(Index));
        }

        if (cuenta.TipoCuenta == "Principal")
        {
            TempData["Error"] = "Las cuentas principales no pueden ser canceladas.";
            return RedirectToAction(nameof(Index));
        }

        var cuentaPrincipal = await _context.CuentasAhorro
            .FirstOrDefaultAsync(c => c.ClienteId == cuenta.ClienteId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (cuentaPrincipal == null)
        {
            TempData["Error"] = "No es posible cancelar la cuenta porque el cliente no tiene una cuenta principal activa para recibir los fondos.";
            return RedirectToAction(nameof(Index));
        }

        if (cuenta.Balance > 0)
        {
            decimal montoTransferencia = cuenta.Balance;
            cuenta.Balance = 0;
            cuentaPrincipal.Balance += montoTransferencia;

            var adminIdStr = _userManager.GetUserId(User);
            int adminId = 0;
            int.TryParse(adminIdStr, out adminId);

            var debito = new Transaccion
            {
                CuentaOrigenId = cuenta.Id,
                Monto = montoTransferencia,
                TipoTransaccion = "DÉBITO",
                Origen = cuenta.NumeroCuenta,
                Beneficiario = cuentaPrincipal.NumeroCuenta,
                Estado = "APROBADA",
                UsuarioResponsableId = adminId,
                FechaTransaccion = DateTime.UtcNow
            };

            var credito = new Transaccion
            {
                CuentaDestinoId = cuentaPrincipal.Id,
                Monto = montoTransferencia,
                TipoTransaccion = "CRÉDITO",
                Origen = cuenta.NumeroCuenta,
                Beneficiario = cuentaPrincipal.NumeroCuenta,
                Estado = "APROBADA",
                UsuarioResponsableId = adminId,
                FechaTransaccion = DateTime.UtcNow
            };

            _context.Transacciones.Add(debito);
            _context.Transacciones.Add(credito);
        }

        cuenta.Estado = "Cancelada";
        await _context.SaveChangesAsync();

        TempData["Success"] = $"La cuenta {cuenta.NumeroCuenta} ha sido cancelada exitosamente.";
        return RedirectToAction(nameof(Index));
    }
}