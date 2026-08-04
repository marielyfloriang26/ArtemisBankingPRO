using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Prestamos;
using ArtemisBankingPro.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class PrestamoService : IPrestamoService
{
    private readonly IPrestamoRepository _prestamoRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ICuentaAhorroRepository _cuentaRepo;
    private readonly ITransaccionRepository _transaccionRepo;
    private readonly IGenericRepository<TarjetaCredito> _tarjetaRepo;
    private readonly ICuotaPrestamoRepository _cuotaRepo;
    private readonly IEmailService _emailService;

    public PrestamoService(
        IPrestamoRepository prestamoRepo,
        IUsuarioRepository usuarioRepo,
        ICuentaAhorroRepository cuentaRepo,
        ITransaccionRepository transaccionRepo,
        IGenericRepository<TarjetaCredito> tarjetaRepo,
        ICuotaPrestamoRepository cuotaRepo,
        IEmailService emailService)
    {
        _prestamoRepo = prestamoRepo;
        _usuarioRepo = usuarioRepo;
        _cuentaRepo = cuentaRepo;
        _transaccionRepo = transaccionRepo;
        _tarjetaRepo = tarjetaRepo;
        _cuotaRepo = cuotaRepo;
        _emailService = emailService;
    }

    public async Task<List<PrestamoViewModel>> GetAllPrestamosFilteredAsync(string? cedula, string estadoFiltro)
    {
        var allPrestamos = await _prestamoRepo.GetAllWithIncludesAsync();

        if (!string.IsNullOrEmpty(cedula))
        {
            allPrestamos = allPrestamos.Where(p => p.Cliente != null && p.Cliente.Cedula == cedula).ToList();
        }

        if (!string.IsNullOrEmpty(estadoFiltro) && estadoFiltro != "Todos")
        {
            allPrestamos = allPrestamos.Where(p => p.Estado == estadoFiltro).ToList();
        }

        var ordered = allPrestamos
            .OrderBy(p => p.Estado == "Activo" ? 0 : 1)
            .ThenByDescending(p => p.FechaCreacion)
            .ToList();

        return ordered.Select(p => new PrestamoViewModel
        {
            Id = p.Id,
            NumeroPrestamo = p.NumeroPrestamo,
            ClienteId = p.ClienteId,
            NombreCliente = p.Cliente != null ? $"{p.Cliente.Nombre} {p.Cliente.Apellido}" : "",
            MontoAprobado = p.MontoAprobado,
            TasaInteresAnual = p.TasaInteresAnual,
            PlazoMeses = p.PlazoMeses,
            MontoPendiente = p.MontoPendiente,
            Estado = p.Estado,
            FechaCreacion = p.FechaCreacion,
            Cuotas = p.Cuotas != null ? p.Cuotas.Select(c => new CuotaPrestamoViewModel
            {
                Id = c.Id,
                PrestamoId = c.PrestamoId,
                NumeroCuota = c.NumeroCuota,
                FechaVencimiento = c.FechaVencimiento,
                ValorCuota = c.ValorCuota,
                MontoInteres = c.MontoInteres,
                MontoCapital = c.MontoCapital,
                SaldoPendiente = c.SaldoPendiente,
                EstadoPago = c.EstadoPago,
                TieneAtraso = c.TieneAtraso
            }).ToList() : new List<CuotaPrestamoViewModel>()
        }).ToList();
    }

    public async Task<List<ClienteElegibleViewModel>> GetClientesElegiblesAsync(string? cedula)
    {
        var clientes = await _usuarioRepo.GetAllClientesAsync();
        clientes = clientes.Where(c => c.EsActivo).ToList();

        if (!string.IsNullOrEmpty(cedula))
        {
            clientes = clientes.Where(c => c.Cedula.Contains(cedula)).ToList();
        }

        var results = new List<ClienteElegibleViewModel>();
        foreach (var c in clientes)
        {
            var prestamoActivo = await _prestamoRepo.GetPrestamoActivoByClienteIdAsync(c.Id);
            if (prestamoActivo == null)
            {
                decimal deuda = await CalcularDeudaTotalClienteAsync(c.Id);
                results.Add(new ClienteElegibleViewModel
                {
                    Id = c.Id,
                    Cedula = c.Cedula,
                    NombreCompleto = $"{c.Nombre} {c.Apellido}",
                    Correo = c.Email ?? "",
                    DeudaTotal = deuda
                });
            }
        }
        return results;
    }

    public async Task<bool> ExisteClienteConCedulaAsync(string cedula)
    {
        var clientes = await _usuarioRepo.GetAllClientesAsync();
        return clientes.Any(c => c.Cedula == cedula);
    }

    public async Task<decimal> CalcularDeudaTotalClienteAsync(int clienteId)
    {
        var prestamoActivo = await _prestamoRepo.GetPrestamoActivoByClienteIdAsync(clienteId);
        decimal deudaPrestamo = prestamoActivo?.MontoPendiente ?? 0m;

        var tarjetas = await _tarjetaRepo.GetAllAsync();
        decimal deudaTarjetas = tarjetas.Where(t => t.ClienteId == clienteId && t.Estado == "Activa").Sum(t => t.MontoAdeudado);

        return deudaPrestamo + deudaTarjetas;
    }

    public async Task<decimal> CalcularDeudaPromedioGlobalAsync()
    {
        var clientesActivos = (await _usuarioRepo.GetAllClientesAsync()).Where(c => c.EsActivo).ToList();
        if (!clientesActivos.Any()) return 0m;

        decimal sumaTotal = 0;
        foreach (var c in clientesActivos)
        {
            sumaTotal += await CalcularDeudaTotalClienteAsync(c.Id);
        }

        return Math.Round(sumaTotal / clientesActivos.Count, 2);
    }

    public async Task<decimal> CalcularDeudaProyectadaAsync(int clienteId, decimal monto, decimal tasaAnual, int plazo)
    {
        decimal deudaActual = await CalcularDeudaTotalClienteAsync(clienteId);
        decimal totalAPagar = CalcularTotalAPagarNuevoPrestamo(monto, tasaAnual, plazo);
        return deudaActual + totalAPagar;
    }

    private decimal CalcularTotalAPagarNuevoPrestamo(decimal monto, decimal tasaAnual, int plazo)
    {
        if (plazo <= 0 || monto <= 0) return 0;
        decimal tasaMensual = (tasaAnual / 100m) / 12m;
        decimal cuota = 0;
        if (tasaMensual == 0)
        {
            cuota = monto / plazo;
        }
        else
        {
            double factor = Math.Pow((double)(1m + tasaMensual), plazo);
            cuota = monto * (tasaMensual * (decimal)factor) / (decimal)(factor - 1);
        }
        cuota = Math.Round(cuota, 2);
        return cuota * plazo;
    }

    private async Task<string> GenerarNumeroPrestamoAsync()
    {
        var rng = new Random();
        while (true)
        {
            string num = rng.Next(100000000, 999999999).ToString();
            var existsPrestamo = await _prestamoRepo.GetByNumeroPrestamoAsync(num);
            var existsCuenta = await _cuentaRepo.GetByNumeroCuentaAsync(num);
            if (existsPrestamo == null && existsCuenta == null) return num;
        }
    }

    public async Task<string> AsignarPrestamoAsync(SavePrestamoViewModel model)
    {
        var cliente = await _usuarioRepo.GetByIdAsync(model.ClienteId);
        if (cliente == null || !cliente.EsActivo) return "El cliente seleccionado no existe o no está activo.";

        var prestamoActivo = await _prestamoRepo.GetPrestamoActivoByClienteIdAsync(model.ClienteId);
        if (prestamoActivo != null) return "Este cliente ya tiene un préstamo activo asignado.";

        var cuentas = await _cuentaRepo.GetByClienteIdAsync(model.ClienteId);
        var cuentaPrincipal = cuentas.FirstOrDefault(c => c.TipoCuenta == "Principal" && c.Estado == "Activa");
        if (cuentaPrincipal == null) return "El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo.";

        string numeroPrestamo = await GenerarNumeroPrestamoAsync();

        var prestamo = new Prestamo
        {
            NumeroPrestamo = numeroPrestamo,
            ClienteId = model.ClienteId,
            MontoAprobado = model.MontoAprobado,
            MontoPendiente = model.MontoAprobado,
            TasaInteresAnual = model.TasaInteresAnual,
            PlazoMeses = model.PlazoMeses,
            Estado = "Activo",
            AdminId = model.AdminId,
            FechaCreacion = DateTime.UtcNow
        };
        await _prestamoRepo.AddAsync(prestamo);

        decimal p = model.MontoAprobado;
        decimal r = (model.TasaInteresAnual / 100m) / 12m;
        int n = model.PlazoMeses;
        decimal cuotaMensual = 0;
        if (r == 0)
            cuotaMensual = p / n;
        else
        {
            double factor = Math.Pow((double)(1m + r), n);
            cuotaMensual = p * (r * (decimal)factor) / (decimal)(factor - 1);
        }
        cuotaMensual = Math.Round(cuotaMensual, 2);

        decimal saldo = p;
        for (int i = 1; i <= n; i++)
        {
            decimal interes = Math.Round(saldo * r, 2);
            decimal capital = Math.Round(cuotaMensual - interes, 2);
            if (i == n) 
            {
                capital = saldo;
                cuotaMensual = capital + interes;
            }

            saldo -= capital;
            
            await _cuotaRepo.AddAsync(new CuotaPrestamo
            {
                PrestamoId = prestamo.Id,
                NumeroCuota = i,
                FechaVencimiento = DateTime.UtcNow.AddMonths(i),
                ValorCuota = cuotaMensual,
                MontoInteres = interes,
                MontoCapital = capital,
                SaldoPendiente = cuotaMensual,
                EstadoPago = "Pendiente",
                TieneAtraso = false
            });
        }

        cuentaPrincipal.Balance += model.MontoAprobado;
        await _cuentaRepo.UpdateAsync(cuentaPrincipal, cuentaPrincipal.Id);

        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaDestinoId = cuentaPrincipal.Id,
            Monto = model.MontoAprobado,
            TipoTransaccion = "CRÉDITO",
            Origen = prestamo.NumeroPrestamo,
            Beneficiario = cuentaPrincipal.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = model.AdminId,
            FechaTransaccion = DateTime.UtcNow
        });

        try {
            await _emailService.SendEmailAsync(cliente.Email, "Préstamo aprobado", $"Número: {prestamo.NumeroPrestamo}, Monto: {model.MontoAprobado}, Plazo: {model.PlazoMeses}, Tasa: {model.TasaInteresAnual}, Cuota: {cuotaMensual}");
        } catch {
            return "El préstamo fue creado correctamente, pero no fue posible enviar el correo de notificación.";
        }

        return string.Empty;
    }

    public async Task<PrestamoViewModel?> GetPrestamoDetailsAsync(int id)
    {
        var p = await _prestamoRepo.GetByIdWithIncludesAsync(id);
        if (p == null) return null;

        return new PrestamoViewModel
        {
            Id = p.Id,
            NumeroPrestamo = p.NumeroPrestamo,
            ClienteId = p.ClienteId,
            NombreCliente = p.Cliente != null ? $"{p.Cliente.Nombre} {p.Cliente.Apellido}" : "",
            MontoAprobado = p.MontoAprobado,
            TasaInteresAnual = p.TasaInteresAnual,
            PlazoMeses = p.PlazoMeses,
            MontoPendiente = p.MontoPendiente,
            Estado = p.Estado,
            FechaCreacion = p.FechaCreacion,
            Cuotas = p.Cuotas != null ? p.Cuotas.Select(c => new CuotaPrestamoViewModel
            {
                Id = c.Id,
                PrestamoId = c.PrestamoId,
                NumeroCuota = c.NumeroCuota,
                FechaVencimiento = c.FechaVencimiento,
                ValorCuota = c.ValorCuota,
                MontoInteres = c.MontoInteres,
                MontoCapital = c.MontoCapital,
                SaldoPendiente = c.SaldoPendiente,
                EstadoPago = c.EstadoPago,
                TieneAtraso = c.TieneAtraso
            }).OrderBy(c => c.NumeroCuota).ToList() : new List<CuotaPrestamoViewModel>()
        };
    }

    public async Task<EditTasaPrestamoViewModel?> GetEditTasaViewModelAsync(int id)
    {
        var p = await _prestamoRepo.GetByIdAsync(id);
        if (p == null) return null;
        return new EditTasaPrestamoViewModel { PrestamoId = p.Id, NuevaTasaInteresAnual = p.TasaInteresAnual };
    }

    public async Task<string> EditTasaInteresAsync(EditTasaPrestamoViewModel model)
    {
        var p = await _prestamoRepo.GetByIdWithIncludesAsync(model.PrestamoId);
        if (p == null) return "El préstamo seleccionado no existe.";
        if (p.Estado != "Activo") return "Solo se puede modificar la tasa de interés de préstamos activos.";
        if (model.NuevaTasaInteresAnual < 0) return "La tasa de interés anual no puede ser negativa.";

        var cuotasFuturas = p.Cuotas?.Where(c => c.FechaVencimiento > DateTime.UtcNow && c.EstadoPago == "Pendiente").OrderBy(c => c.NumeroCuota).ToList();
        if (cuotasFuturas == null || !cuotasFuturas.Any()) return "No existen cuotas futuras pendientes para recalcular.";

        p.TasaInteresAnual = model.NuevaTasaInteresAnual;
        await _prestamoRepo.UpdateAsync(p, p.Id);

        decimal saldoRestante = cuotasFuturas.First().MontoCapital + cuotasFuturas.Skip(1).Sum(c => c.MontoCapital); // Wait, this is just remaining principal. We can compute remaining principal by summing MontoCapital of all pending quotas.
        
        decimal r = (model.NuevaTasaInteresAnual / 100m) / 12m;
        int n = cuotasFuturas.Count;
        decimal cuotaMensual = 0;
        if (r == 0)
            cuotaMensual = saldoRestante / n;
        else
        {
            double factor = Math.Pow((double)(1m + r), n);
            cuotaMensual = saldoRestante * (r * (decimal)factor) / (decimal)(factor - 1);
        }
        cuotaMensual = Math.Round(cuotaMensual, 2);

        decimal saldo = saldoRestante;
        foreach (var c in cuotasFuturas)
        {
            decimal interes = Math.Round(saldo * r, 2);
            decimal capital = Math.Round(cuotaMensual - interes, 2);
            if (c == cuotasFuturas.Last())
            {
                capital = saldo;
                cuotaMensual = capital + interes;
            }
            saldo -= capital;

            c.ValorCuota = cuotaMensual;
            c.MontoInteres = interes;
            c.MontoCapital = capital;
            c.SaldoPendiente = cuotaMensual;
            await _cuotaRepo.UpdateAsync(c, c.Id);
        }

        try {
            await _emailService.SendEmailAsync(p.Cliente?.Email ?? "", "Actualización de tasa de interés de préstamo", $"Número: {p.NumeroPrestamo}, Nueva tasa: {model.NuevaTasaInteresAnual}, Nuevo valor de próxima cuota: {cuotasFuturas.First().ValorCuota}, Fecha vencimiento próxima cuota: {cuotasFuturas.First().FechaVencimiento}");
        } catch { }

        return string.Empty;
    }
}
