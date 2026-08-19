using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Admin;
using ArtemisBankingPro.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Persistence.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminHomeViewModel> GetDashboardIndicatorsAsync()
    {
        var hoy = DateTime.UtcNow.Date;

        // 1. Transacciones (Solo las APROBADAS cuentan)
        var transHistoricas = await _context.Transacciones.Where(t => t.Estado == "APROBADA").ToListAsync();
        
        // 2. Pagos: Filtramos origen que indique pago a préstamo o tarjeta
        // Según tu entidad, 'Origen' puede contener el nº de préstamo o tarjeta
        var esPago = (string t) => t.StartsWith("PAGOPRESTAMO") || t.StartsWith("PAGOTARJETA");
        
        var totalPagos = transHistoricas.Count(t => esPago(t.Origen));
        var pagosHoy = transHistoricas.Count(t => esPago(t.Origen) && t.FechaTransaccion.Date == hoy);

        // 3. Clientes
        var clientes = await _context.Users.Where(u => u.TipoUsuario == "Cliente").ToListAsync();
        var cActivos = clientes.Count(c => c.EsActivo);
        var cInactivos = clientes.Count(c => !c.EsActivo);

        // 4. Productos Financieros Activos
        var ctasAhorro = await _context.CuentasAhorro.Where(c => c.Estado == "Activa").CountAsync();
        var prestamos = await _context.Prestamos.Where(p => p.Estado == "Activo").CountAsync();
        var tarjetas = await _context.TarjetasCredito.Where(t => t.Estado == "Activa").CountAsync();

        // 5. Monto Promedio Deuda (Solo clientes activos)
        decimal deudaTotal = 0;
        if (cActivos > 0)
        {
            var deudaPrestamos = await _context.Prestamos.Where(p => p.Estado == "Activo").SumAsync(p => p.MontoPendiente);
            var deudaTarjetas = await _context.TarjetasCredito.Where(t => t.Estado == "Activa").SumAsync(t => t.MontoAdeudado);
            deudaTotal = deudaPrestamos + deudaTarjetas;
        }

        return new AdminHomeViewModel
        {
            TotalTransaccionesHistoricas = transHistoricas.Count,
            TransaccionesDelDia = transHistoricas.Count(t => t.FechaTransaccion.Date == hoy),
            TotalPagosHistoricos = totalPagos,
            PagosDelDia = pagosHoy,
            ClientesActivos = cActivos,
            ClientesInactivos = cInactivos,
            CuentasAhorroActivas = ctasAhorro,
            PrestamosVigentes = prestamos,
            TarjetasCreditoActivas = tarjetas,
            TotalProductosFinancierosActivos = ctasAhorro + prestamos + tarjetas,
            MontoPromedioDeudaPorCliente = cActivos > 0 ? (deudaTotal / cActivos) : 0
        };
    }
}