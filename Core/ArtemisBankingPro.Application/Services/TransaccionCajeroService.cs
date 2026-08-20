using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Cajero;
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
    private readonly ITarjetaCreditoRepository _tarjetaRepo;
    private readonly IEmailService _emailService;

    public TransaccionCajeroService(
        ICuentaAhorroRepository cuentaRepo,
        IPrestamoRepository prestamoRepo,
        ICuotaPrestamoRepository cuotaRepo,
        ITransaccionRepository transaccionRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService emailService, ITarjetaCreditoRepository tarjetaCreditoRepository)
    {
        _cuentaRepo = cuentaRepo;
        _prestamoRepo = prestamoRepo;
        _cuotaRepo = cuotaRepo;
        _transaccionRepo = transaccionRepo;
        _usuarioRepo = usuarioRepo;
        _emailService = emailService;
        _tarjetaRepo = tarjetaCreditoRepository;
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

    private async Task RegistrarTransaccionRechazadaAsync(int cuentaOrigenId, string numeroCuentaOrigen, decimal monto, string beneficiario, int cajeroId, string tipoOperacion)
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
            FechaTransaccion = DateTime.UtcNow,
            TipoOperacion = tipoOperacion
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
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, numeroPrestamo, cajeroId, "PAGO_PRESTAMO");
            return ("El número de préstamo ingresado no corresponde a un préstamo válido.", null, null, null, 0m);
        }

        var prestamo = await _prestamoRepo.GetByNumeroPrestamoAsync(numeroPrestamo);
        if (prestamo == null || prestamo.Estado != "Activo")
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, numeroPrestamo, cajeroId, "PAGO_PRESTAMO");
            return ("El número de préstamo ingresado no corresponde a un préstamo válido.", null, null, null, 0m);
        }

        var cuotas = await _cuotaRepo.GetByPrestamoIdAsync(prestamo.Id);
        var cuotasPendientes = cuotas.Where(c => c.EstadoPago != "Pagada").OrderBy(c => c.NumeroCuota).ToList();

        if (!cuotasPendientes.Any())
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, prestamo.NumeroPrestamo, cajeroId, "PAGO_PRESTAMO");
            return ("El préstamo seleccionado no tiene cuotas pendientes de pago.", null, null, null, 0m);
        }

        decimal montoPendienteReal = cuotasPendientes.Sum(c => c.SaldoPendiente);
        decimal montoEfectivo = Math.Min(monto, montoPendienteReal);

        if (origen.Balance < montoEfectivo)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, prestamo.NumeroPrestamo, cajeroId, "PAGO_PRESTAMO");
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

        // El monto pendiente del prestamo refleja el saldo real (capital + interes) de las cuotas no pagadas
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
            FechaTransaccion = fecha,
            TipoOperacion = "PAGO_PRESTAMO"
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
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, numeroCuentaDestino, cajeroId, "TRANSFERENCIA_TERCEROS");
            return ("El número de cuenta destino ingresado no corresponde a una cuenta válida.", null, null);
        }

        if (origen.Id == destino.Id)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, destino.NumeroCuenta, cajeroId, "TRANSFERENCIA_TERCEROS");
            return ("La cuenta origen y la cuenta destino no pueden ser la misma.", null, null);
        }

        if (origen.Balance < monto)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, destino.NumeroCuenta, cajeroId, "TRANSFERENCIA_TERCEROS");
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
            FechaTransaccion = fecha,
            TipoOperacion = "TRANSFERENCIA_TERCEROS"
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
            FechaTransaccion = fecha,
            TipoOperacion = "TRANSFERENCIA_TERCEROS"
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
    #region Depósito

private async Task<(string? error, CuentaAhorro? cuenta)> ValidarDepositoAsync(string numeroCuenta, decimal monto)
{
    var cuenta = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuenta);
    if (cuenta == null || cuenta.Estado != "Activa")
        return ("El número de cuenta ingresado no corresponde a una cuenta válida.", null);

    return (null, cuenta);
}

public async Task<(string? error, DepositoViewModel? confirm)> PreviewDepositoAsync(int cajeroId, DepositoViewModel model)
{
    var (error, cuenta) = await ValidarDepositoAsync(model.NumeroCuenta, model.Monto);
    if (error != null) return (error, null);

    var titular = await _usuarioRepo.GetByIdAsync(cuenta!.ClienteId);
    model.CuentaId = cuenta.Id;
    model.TitularCuenta = titular != null ? NombreCompleto(titular) : "";
    return (null, model);
}

public async Task<OperationResultViewModel> EjecutarDepositoAsync(int cajeroId, DepositoViewModel model)
{
    var (error, cuenta) = await ValidarDepositoAsync(model.NumeroCuenta, model.Monto);
    if (error != null) return new OperationResultViewModel { Success = false, Message = error };

    cuenta!.Balance += model.Monto;
    await _cuentaRepo.UpdateAsync(cuenta, cuenta.Id);

    var fecha = DateTime.UtcNow;
    await _transaccionRepo.AddAsync(new Transaccion
    {
        CuentaDestinoId = cuenta.Id,
        Monto = model.Monto,
        TipoTransaccion = "CRÉDITO",
        Origen = "DEPÓSITO",
        Beneficiario = cuenta.NumeroCuenta,
        Estado = "APROBADA",
        UsuarioResponsableId = cajeroId,
        FechaTransaccion = fecha,
        TipoOperacion = "DEPOSITO"
    });

    var titular = await _usuarioRepo.GetByIdAsync(cuenta.ClienteId);
    bool correoOk = await EnviarCorreoSeguroAsync(titular?.Email,
        $"Depósito realizado a su cuenta {UltimosDigitos(cuenta.NumeroCuenta)}",
        $"Se ha realizado un depósito a su cuenta terminada en {UltimosDigitos(cuenta.NumeroCuenta)}. Monto depositado: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}.");

    return new OperationResultViewModel
    {
        Success = true,
        CorreoFallido = !correoOk,
        Message = correoOk ? "Depósito realizado correctamente." : "El depósito fue realizado correctamente, pero no fue posible enviar el correo de notificación."
    };
}

#endregion

#region Retiro

private async Task<(string? error, CuentaAhorro? cuenta)> ValidarRetiroAsync(int cajeroId, string numeroCuenta, decimal monto)
{
    var cuenta = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuenta);
    if (cuenta == null || cuenta.Estado != "Activa")
        return ("El número de cuenta ingresado no corresponde a una cuenta válida.", null);

    if (cuenta.Balance < monto)
    {
        await RegistrarTransaccionRechazadaAsync(cuenta.Id, cuenta.NumeroCuenta, monto, "RETIRO", cajeroId, "RETIRO");
        return ("El monto ingresado excede el saldo disponible de la cuenta.", null);
    }

    return (null, cuenta);
}

public async Task<(string? error, RetiroViewModel? confirm)> PreviewRetiroAsync(int cajeroId, RetiroViewModel model)
{
    var (error, cuenta) = await ValidarRetiroAsync(cajeroId, model.NumeroCuenta, model.Monto);
    if (error != null) return (error, null);

    var titular = await _usuarioRepo.GetByIdAsync(cuenta!.ClienteId);
    model.CuentaId = cuenta.Id;
    model.TitularCuenta = titular != null ? NombreCompleto(titular) : "";
    return (null, model);
}

public async Task<OperationResultViewModel> EjecutarRetiroAsync(int cajeroId, RetiroViewModel model)
{
    var (error, cuenta) = await ValidarRetiroAsync(cajeroId, model.NumeroCuenta, model.Monto);
    if (error != null) return new OperationResultViewModel { Success = false, Message = error };

    cuenta!.Balance -= model.Monto;
    await _cuentaRepo.UpdateAsync(cuenta, cuenta.Id);

    var fecha = DateTime.UtcNow;
    await _transaccionRepo.AddAsync(new Transaccion
    {
        CuentaOrigenId = cuenta.Id,
        Monto = model.Monto,
        TipoTransaccion = "DÉBITO",
        Origen = cuenta.NumeroCuenta,
        Beneficiario = "RETIRO",
        Estado = "APROBADA",
        UsuarioResponsableId = cajeroId,
        FechaTransaccion = fecha,
        TipoOperacion = "RETIRO"
    });

    var titular = await _usuarioRepo.GetByIdAsync(cuenta.ClienteId);
    bool correoOk = await EnviarCorreoSeguroAsync(titular?.Email,
        $"Retiro realizado desde su cuenta {UltimosDigitos(cuenta.NumeroCuenta)}",
        $"Se ha realizado un retiro desde su cuenta terminada en {UltimosDigitos(cuenta.NumeroCuenta)}. Monto retirado: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}.");

    return new OperationResultViewModel
    {
        Success = true,
        CorreoFallido = !correoOk,
        Message = correoOk ? "Retiro realizado correctamente." : "El retiro fue realizado correctamente, pero no fue posible enviar el correo de notificación."
    };
}

#endregion

#region Pago a tarjeta de crédito (Cajero)

private async Task<(string? error, CuentaAhorro? origen, TarjetaCredito? tarjeta, decimal montoEfectivo)> ValidarPagoTarjetaCajeroAsync(int cajeroId, string numeroCuentaOrigen, string numeroTarjeta, decimal monto)
{
    var origen = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuentaOrigen);
    if (origen == null || origen.Estado != "Activa")
        return ("El número de cuenta ingresado no corresponde a una cuenta válida.", null, null, 0m);

    var tarjeta = await _tarjetaRepo.GetByNumeroTarjetaAsync(numeroTarjeta);
    if (tarjeta == null || tarjeta.Estado != "Activa")
        return ("El número de tarjeta ingresado no corresponde a una tarjeta válida.", null, null, 0m);

    if (tarjeta.MontoAdeudado <= 0)
        return ("La tarjeta seleccionada no tiene deuda pendiente.", null, null, 0m);

    decimal montoEfectivo = Math.Min(monto, tarjeta.MontoAdeudado);

    if (origen.Balance < montoEfectivo)
    {
        await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, UltimosDigitos(tarjeta.NumeroTarjeta), cajeroId, "PAGO_TARJETA");
        return ("El monto ingresado excede el saldo disponible de la cuenta.", null, null, 0m);
    }

    return (null, origen, tarjeta, montoEfectivo);
}

public async Task<(string? error, PagoTarjetaViewModel? confirm)> PreviewPagoTarjetaCajeroAsync(int cajeroId, PagoTarjetaViewModel model)
{
    var (error, origen, tarjeta, montoEfectivo) = await ValidarPagoTarjetaCajeroAsync(cajeroId, model.NumeroCuenta, model.NumeroTarjeta, model.Monto);
    if (error != null) return (error, null);

    var titularCuenta = await _usuarioRepo.GetByIdAsync(origen!.ClienteId);
    var titularTarjeta = await _usuarioRepo.GetByIdAsync(tarjeta!.ClienteId);

    model.CuentaOrigenId = origen.Id;
    model.TitularCuentaOrigen = titularCuenta != null ? NombreCompleto(titularCuenta) : "";
    model.TarjetaId = tarjeta.Id;
    model.TitularTarjeta = titularTarjeta != null ? NombreCompleto(titularTarjeta) : "";
    model.MontoEfectivo = montoEfectivo;
    return (null, model);
}

public async Task<OperationResultViewModel> EjecutarPagoTarjetaCajeroAsync(int cajeroId, PagoTarjetaViewModel model)
{
    var (error, origen, tarjeta, montoEfectivo) = await ValidarPagoTarjetaCajeroAsync(cajeroId, model.NumeroCuenta, model.NumeroTarjeta, model.Monto);
    if (error != null) return new OperationResultViewModel { Success = false, Message = error };

    origen!.Balance -= montoEfectivo;
    await _cuentaRepo.UpdateAsync(origen, origen.Id);

    tarjeta!.MontoAdeudado -= montoEfectivo;
    if (tarjeta.MontoAdeudado < 0) tarjeta.MontoAdeudado = 0m;
    await _tarjetaRepo.UpdateAsync(tarjeta, tarjeta.Id);

    var fecha = DateTime.UtcNow;
    await _transaccionRepo.AddAsync(new Transaccion
    {
        CuentaOrigenId = origen.Id,
        Monto = montoEfectivo,
        TipoTransaccion = "DÉBITO",
        Origen = origen.NumeroCuenta,
        Beneficiario = UltimosDigitos(tarjeta.NumeroTarjeta),
        Estado = "APROBADA",
        UsuarioResponsableId = cajeroId,
        FechaTransaccion = fecha,
        TipoOperacion = "PAGO_TARJETA"
    });

    var titularTarjeta = await _usuarioRepo.GetByIdAsync(tarjeta.ClienteId);
    bool correoOk = await EnviarCorreoSeguroAsync(titularTarjeta?.Email,
        $"Pago realizado a la tarjeta {UltimosDigitos(tarjeta.NumeroTarjeta)}",
        $"Se ha realizado un pago a su tarjeta de crédito terminada en {UltimosDigitos(tarjeta.NumeroTarjeta)}. Monto pagado: {FormatoMonto(montoEfectivo)}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Fecha y hora: {fecha}.");

    if (origen.ClienteId != tarjeta.ClienteId)
    {
        var titularCuenta = await _usuarioRepo.GetByIdAsync(origen.ClienteId);
        bool correoCuentaOk = await EnviarCorreoSeguroAsync(titularCuenta?.Email,
            $"Débito realizado desde su cuenta {UltimosDigitos(origen.NumeroCuenta)}",
            $"Se ha debitado {FormatoMonto(montoEfectivo)} de su cuenta terminada en {UltimosDigitos(origen.NumeroCuenta)} para pagar la tarjeta terminada en {UltimosDigitos(tarjeta.NumeroTarjeta)}. Fecha y hora: {fecha}.");
        correoOk &= correoCuentaOk;
    }

    return new OperationResultViewModel
    {
        Success = true,
        CorreoFallido = !correoOk,
        Message = correoOk ? "Pago realizado correctamente." : "El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación."
    };
}

#endregion

    #region Indicadores Home

    public async Task<(int transaccionesHoy, int pagosHoy, int depositosHoy, int retirosHoy)> GetIndicadoresHomeAsync(int cajeroId)
    {
        var todas = await _transaccionRepo.GetAllAsync();
        var hoy = DateTime.UtcNow.Date;

        var delCajeroHoy = todas.Where(t =>
            t.UsuarioResponsableId == cajeroId &&
            t.Estado == "APROBADA" &&
            t.FechaTransaccion.Date == hoy).ToList();

        int depositos = delCajeroHoy.Count(t => t.TipoOperacion == "DEPOSITO");
        int retiros = delCajeroHoy.Count(t => t.TipoOperacion == "RETIRO");
        int pagosTarjeta = delCajeroHoy.Count(t => t.TipoOperacion == "PAGO_TARJETA");
        int pagosPrestamo = delCajeroHoy.Count(t => t.TipoOperacion == "PAGO_PRESTAMO");
        // TransacciOn a terceros genera 2 filas (DEBITO + CREDITO) solo contamos la fila DEBITO como 1 operacion
        int terceros = delCajeroHoy.Count(t => t.TipoOperacion == "TRANSFERENCIA_TERCEROS" && t.TipoTransaccion == "DÉBITO");

        int pagos = pagosTarjeta + pagosPrestamo;
        int transacciones = depositos + retiros + pagos + terceros;

        return (transacciones, pagos, depositos, retiros);
    }

    #endregion
}
