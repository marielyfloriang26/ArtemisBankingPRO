using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Transacciones;
using ArtemisBankingPro.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class TransaccionCajeroService : ITransaccionCajeroService
{
    private const string MensajeCorreoFallido = "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación.";
    private const string MensajeCorreoFallidoTerceros = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";

    private readonly ICuentaAhorroRepository _cuentaRepo;
    private readonly IPrestamoRepository _prestamoRepo;
    private readonly ICuotaPrestamoRepository _cuotaRepo;
    private readonly ITransaccionRepository _transaccionRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _emailService;

    public TransaccionCajeroService(
        ICuentaAhorroRepository cuentaRepo,
        IPrestamoRepository prestamoRepo,
        ICuotaPrestamoRepository cuotaRepo,
        ITransaccionRepository transaccionRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService emailService)
    {
        _cuentaRepo = cuentaRepo;
        _prestamoRepo = prestamoRepo;
        _cuotaRepo = cuotaRepo;
        _transaccionRepo = transaccionRepo;
        _usuarioRepo = usuarioRepo;
        _emailService = emailService;
    }

    #region Helpers

    private static string UltimosDigitos(string numero, int cantidad = 4)
    {
        if (string.IsNullOrEmpty(numero) || numero.Length <= cantidad) return numero;
        return numero.Substring(numero.Length - cantidad);
    }

    private static string FormatoMonto(decimal monto) => $"RD${monto:N2}";

    private static string NombreCompleto(Usuario usuario) => $"{usuario.Nombre} {usuario.Apellido}";

    private static bool EsNumeroPrestamoValido(string numeroPrestamo) =>
        !string.IsNullOrEmpty(numeroPrestamo) && numeroPrestamo.Length == 9 && numeroPrestamo.All(char.IsDigit);

    private async Task RegistrarTransaccionRechazadaAsync(int cuentaOrigenId, string numeroCuentaOrigen, decimal monto, string beneficiario, int cajeroId)
    {
        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaOrigenId = cuentaOrigenId,
            Monto = monto,
            TipoTransaccion = "DÉBITO",
            Origen = numeroCuentaOrigen,
            Beneficiario = beneficiario,
            Estado = "RECHAZADA",
            UsuarioResponsableId = cajeroId,
            FechaTransaccion = DateTime.UtcNow
        });
    }

    private async Task<bool> EnviarCorreoSeguroAsync(string? to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to)) return false;
        try
        {
            await _emailService.SendEmailAsync(to, subject, body);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Pago a préstamo

    private async Task<(string? error, CuentaAhorro? origen, Prestamo? prestamo, List<CuotaPrestamo>? cuotasPendientes, decimal montoEfectivo)> ValidarPagoPrestamoAsync(int cajeroId, string numeroCuentaOrigen, string numeroPrestamo, decimal monto)
    {
        var origen = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuentaOrigen);
        if (origen == null || origen.Estado != "Activa")
            return ("El número de cuenta ingresado no corresponde a una cuenta válida.", null, null, null, 0m);

        if (!EsNumeroPrestamoValido(numeroPrestamo))
            return ("El número de préstamo ingresado no corresponde a un préstamo válido.", null, null, null, 0m);

        var prestamo = await _prestamoRepo.GetByNumeroPrestamoAsync(numeroPrestamo);
        if (prestamo == null || prestamo.Estado != "Activo")
            return ("El número de préstamo ingresado no corresponde a un préstamo válido.", null, null, null, 0m);

        var cuotas = await _cuotaRepo.GetByPrestamoIdAsync(prestamo.Id);
        var cuotasPendientes = cuotas.Where(c => c.EstadoPago != "Pagada").OrderBy(c => c.NumeroCuota).ToList();

        if (!cuotasPendientes.Any())
            return ("El préstamo seleccionado no tiene cuotas pendientes de pago.", null, null, null, 0m);

        decimal montoPendienteReal = cuotasPendientes.Sum(c => c.SaldoPendiente);
        decimal montoEfectivo = Math.Min(monto, montoPendienteReal);

        if (origen.Balance < montoEfectivo)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, prestamo.NumeroPrestamo, cajeroId);
            return ("El monto ingresado excede el saldo disponible de la cuenta.", null, null, null, 0m);
        }

        return (null, origen, prestamo, cuotasPendientes, montoEfectivo);
    }

    public async Task<(string? error, PagoPrestamoCajeroConfirmViewModel? confirm)> PreviewPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, prestamo, _, montoEfectivo) = await ValidarPagoPrestamoAsync(cajeroId, model.NumeroCuentaOrigen, model.NumeroPrestamo, monto);
        if (error != null) return (error, null);

        var titularCuenta = await _usuarioRepo.GetByIdAsync(origen!.ClienteId);
        var titularPrestamo = await _usuarioRepo.GetByIdAsync(prestamo!.ClienteId);

        var confirm = new PagoPrestamoCajeroConfirmViewModel
        {
            CuentaOrigenId = origen.Id,
            NumeroCuentaOrigen = origen.NumeroCuenta,
            TitularCuentaOrigen = titularCuenta != null ? NombreCompleto(titularCuenta) : "",
            PrestamoId = prestamo.Id,
            NumeroPrestamo = prestamo.NumeroPrestamo,
            TitularPrestamo = titularPrestamo != null ? NombreCompleto(titularPrestamo) : "",
            MontoIngresado = monto,
            MontoEfectivo = montoEfectivo
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarPagoPrestamoAsync(int cajeroId, PagoPrestamoCajeroConfirmViewModel model)
    {
        var (error, origen, prestamo, cuotasPendientes, montoEfectivo) = await ValidarPagoPrestamoAsync(cajeroId, model.NumeroCuentaOrigen, model.NumeroPrestamo, model.MontoIngresado);
        if (error != null) return new OperationResultViewModel { Success = false, Message = error };

        origen!.Balance -= montoEfectivo;
        await _cuentaRepo.UpdateAsync(origen, origen.Id);

        decimal restante = montoEfectivo;
        foreach (var cuota in cuotasPendientes!)
        {
            if (restante <= 0) break;

            if (cuota.SaldoPendiente <= restante)
            {
                restante -= cuota.SaldoPendiente;
                cuota.SaldoPendiente = 0m;
                cuota.EstadoPago = "Pagada";
                cuota.TieneAtraso = false;
            }
            else
            {
                cuota.SaldoPendiente -= restante;
                restante = 0m;
                cuota.EstadoPago = "ParcialmentePagada";
            }

            await _cuotaRepo.UpdateAsync(cuota, cuota.Id);
        }

        // El monto pendiente del préstamo refleja el saldo real (capital + interés) de las cuotas no pagadas.
        prestamo!.MontoPendiente = cuotasPendientes.Sum(c => c.SaldoPendiente);

        if (prestamo.MontoPendiente <= 0)
        {
            prestamo.Estado = "Completado";
        }

        await _prestamoRepo.UpdateAsync(prestamo, prestamo.Id);

        var fecha = DateTime.UtcNow;
        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaOrigenId = origen.Id,
            Monto = montoEfectivo,
            TipoTransaccion = "DÉBITO",
            Origen = origen.NumeroCuenta,
            Beneficiario = prestamo.NumeroPrestamo,
            Estado = "APROBADA",
            UsuarioResponsableId = cajeroId,
            FechaTransaccion = fecha
        });

        var titularPrestamo = await _usuarioRepo.GetByIdAsync(prestamo.ClienteId);
        bool correoOk = await EnviarCorreoSeguroAsync(titularPrestamo?.Email,
            $"Pago realizado al préstamo {prestamo.NumeroPrestamo}",
            $"Monto pagado: {FormatoMonto(montoEfectivo)}. Número de préstamo: {prestamo.NumeroPrestamo}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Fecha y hora: {fecha}.");

        if (origen.ClienteId != prestamo.ClienteId)
        {
            var titularCuenta = await _usuarioRepo.GetByIdAsync(origen.ClienteId);
            bool correoCuentaOk = await EnviarCorreoSeguroAsync(titularCuenta?.Email,
                $"Débito realizado desde su cuenta {UltimosDigitos(origen.NumeroCuenta)}",
                $"Se ha debitado {FormatoMonto(montoEfectivo)} de su cuenta terminada en {UltimosDigitos(origen.NumeroCuenta)} para realizar un pago al préstamo {prestamo.NumeroPrestamo}. Fecha y hora: {fecha}.");
            correoOk &= correoCuentaOk;
        }

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Pago realizado correctamente." : MensajeCorreoFallido
        };
    }

    #endregion

    #region Transacciones a cuentas de terceros

    private async Task<(string? error, CuentaAhorro? origen, CuentaAhorro? destino)> ValidarTransaccionTercerosAsync(int cajeroId, string numeroCuentaOrigen, string numeroCuentaDestino, decimal monto)
    {
        var origen = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuentaOrigen);
        if (origen == null || origen.Estado != "Activa")
            return ("El número de cuenta origen ingresado no corresponde a una cuenta válida.", null, null);

        var destino = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuentaDestino);
        if (destino == null || destino.Estado != "Activa")
            return ("El número de cuenta destino ingresado no corresponde a una cuenta válida.", null, null);

        if (origen.Id == destino.Id)
            return ("La cuenta origen y la cuenta destino no pueden ser la misma.", null, null);

        if (origen.Balance < monto)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, destino.NumeroCuenta, cajeroId);
            return ("El monto ingresado excede el saldo disponible de la cuenta.", null, null);
        }

        return (null, origen, destino);
    }

    public async Task<(string? error, TransaccionTercerosCajeroConfirmViewModel? confirm)> PreviewTransaccionTercerosAsync(int cajeroId, TransaccionTercerosCajeroFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, destino) = await ValidarTransaccionTercerosAsync(cajeroId, model.NumeroCuentaOrigen, model.NumeroCuentaDestino, monto);
        if (error != null) return (error, null);

        var titularOrigen = await _usuarioRepo.GetByIdAsync(origen!.ClienteId);
        var titularDestino = await _usuarioRepo.GetByIdAsync(destino!.ClienteId);

        var confirm = new TransaccionTercerosCajeroConfirmViewModel
        {
            CuentaOrigenId = origen.Id,
            NumeroCuentaOrigen = origen.NumeroCuenta,
            TitularCuentaOrigen = titularOrigen != null ? NombreCompleto(titularOrigen) : "",
            CuentaDestinoId = destino.Id,
            NumeroCuentaDestino = destino.NumeroCuenta,
            TitularCuentaDestino = titularDestino != null ? NombreCompleto(titularDestino) : "",
            Monto = monto
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarTransaccionTercerosAsync(int cajeroId, TransaccionTercerosCajeroConfirmViewModel model)
    {
        var (error, origen, destino) = await ValidarTransaccionTercerosAsync(cajeroId, model.NumeroCuentaOrigen, model.NumeroCuentaDestino, model.Monto);
        if (error != null) return new OperationResultViewModel { Success = false, Message = error };

        origen!.Balance -= model.Monto;
        await _cuentaRepo.UpdateAsync(origen, origen.Id);

        destino!.Balance += model.Monto;
        await _cuentaRepo.UpdateAsync(destino, destino.Id);

        var fecha = DateTime.UtcNow;

        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaOrigenId = origen.Id,
            Monto = model.Monto,
            TipoTransaccion = "DÉBITO",
            Origen = origen.NumeroCuenta,
            Beneficiario = destino.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = cajeroId,
            FechaTransaccion = fecha
        });

        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaDestinoId = destino.Id,
            Monto = model.Monto,
            TipoTransaccion = "CRÉDITO",
            Origen = origen.NumeroCuenta,
            Beneficiario = destino.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = cajeroId,
            FechaTransaccion = fecha
        });

        var titularOrigen = await _usuarioRepo.GetByIdAsync(origen.ClienteId);
        var titularDestino = await _usuarioRepo.GetByIdAsync(destino.ClienteId);

        bool correoOk = true;
        correoOk &= await EnviarCorreoSeguroAsync(titularOrigen?.Email,
            $"Transacción realizada a la cuenta {UltimosDigitos(destino.NumeroCuenta)}",
            $"Monto transferido: {FormatoMonto(model.Monto)}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Cuenta destino terminada en: {UltimosDigitos(destino.NumeroCuenta)}. Fecha y hora: {fecha}.");
        correoOk &= await EnviarCorreoSeguroAsync(titularDestino?.Email,
            $"Transacción enviada desde la cuenta {UltimosDigitos(origen.NumeroCuenta)}",
            $"Monto recibido: {FormatoMonto(model.Monto)}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Cuenta destino terminada en: {UltimosDigitos(destino.NumeroCuenta)}. Fecha y hora: {fecha}.");

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Transacción realizada correctamente." : MensajeCorreoFallidoTerceros
        };
    }

    #endregion
}
