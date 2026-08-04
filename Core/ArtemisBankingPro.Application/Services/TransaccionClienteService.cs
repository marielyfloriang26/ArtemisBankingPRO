using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Application.ViewModels.Transacciones;
using ArtemisBankingPro.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Services;

public class TransaccionClienteService : ITransaccionClienteService
{
    private const string MensajeCorreoFallido = "La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo.";

    private readonly ICuentaAhorroRepository _cuentaRepo;
    private readonly ITarjetaCreditoRepository _tarjetaRepo;
    private readonly IPrestamoRepository _prestamoRepo;
    private readonly ICuotaPrestamoRepository _cuotaRepo;
    private readonly IBeneficiarioRepository _beneficiarioRepo;
    private readonly ITransaccionRepository _transaccionRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IEmailService _emailService;

    public TransaccionClienteService(
        ICuentaAhorroRepository cuentaRepo,
        ITarjetaCreditoRepository tarjetaRepo,
        IPrestamoRepository prestamoRepo,
        ICuotaPrestamoRepository cuotaRepo,
        IBeneficiarioRepository beneficiarioRepo,
        ITransaccionRepository transaccionRepo,
        IUsuarioRepository usuarioRepo,
        IEmailService emailService)
    {
        _cuentaRepo = cuentaRepo;
        _tarjetaRepo = tarjetaRepo;
        _prestamoRepo = prestamoRepo;
        _cuotaRepo = cuotaRepo;
        _beneficiarioRepo = beneficiarioRepo;
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

    private async Task RegistrarTransaccionRechazadaAsync(int cuentaOrigenId, string numeroCuentaOrigen, decimal monto, string beneficiario, int clienteId)
    {
        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaOrigenId = cuentaOrigenId,
            Monto = monto,
            TipoTransaccion = "DÉBITO",
            Origen = numeroCuentaOrigen,
            Beneficiario = beneficiario,
            Estado = "RECHAZADA",
            UsuarioResponsableId = clienteId,
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

    #region Transacción Express

    public async Task<ExpressFormViewModel> BuildExpressFormAsync(int clienteId)
    {
        var cuentasOrigen = await GetCuentasActivasSelectAsync(clienteId);
        return new ExpressFormViewModel { CuentasOrigen = cuentasOrigen };
    }

    private async Task<(string? error, CuentaAhorro? origen, CuentaAhorro? destino)> ValidarExpressAsync(int clienteId, int cuentaOrigenId, string numeroCuentaDestino, decimal monto)
    {
        var origen = await _cuentaRepo.GetByIdAsync(cuentaOrigenId);
        if (origen == null || origen.ClienteId != clienteId || origen.Estado != "Activa")
            return ("La cuenta de origen seleccionada no es válida.", null, null);

        var destino = await _cuentaRepo.GetByNumeroCuentaAsync(numeroCuentaDestino);
        if (destino == null || destino.Estado != "Activa")
            return ("El número de cuenta ingresado no corresponde a una cuenta válida.", null, null);

        if (origen.Id == destino.Id)
            return ("La cuenta destino no puede ser la misma cuenta de origen.", null, null);

        if (origen.Balance < monto)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, destino.NumeroCuenta, clienteId);
            return ("El monto ingresado excede el saldo disponible de la cuenta seleccionada.", null, null);
        }

        return (null, origen, destino);
    }

    public async Task<(string? error, ExpressConfirmViewModel? confirm)> PreviewExpressAsync(int clienteId, ExpressFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, destino) = await ValidarExpressAsync(clienteId, model.CuentaOrigenId ?? 0, model.NumeroCuentaDestino, monto);
        if (error != null) return (error, null);

        var titular = await _usuarioRepo.GetByIdAsync(destino!.ClienteId);
        var confirm = new ExpressConfirmViewModel
        {
            CuentaOrigenId = origen!.Id,
            NumeroCuentaOrigen = origen.NumeroCuenta,
            NumeroCuentaDestino = destino.NumeroCuenta,
            Monto = monto,
            TitularCuentaDestino = titular != null ? NombreCompleto(titular) : ""
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarExpressAsync(int clienteId, ExpressConfirmViewModel model)
    {
        var (error, origen, destino) = await ValidarExpressAsync(clienteId, model.CuentaOrigenId, model.NumeroCuentaDestino, model.Monto);
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
            UsuarioResponsableId = clienteId,
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
            UsuarioResponsableId = clienteId,
            FechaTransaccion = fecha
        });

        var remitente = await _usuarioRepo.GetByIdAsync(clienteId);
        var receptor = await _usuarioRepo.GetByIdAsync(destino.ClienteId);

        bool correoOk = true;
        correoOk &= await EnviarCorreoSeguroAsync(remitente?.Email,
            $"Transacción realizada a la cuenta {UltimosDigitos(destino.NumeroCuenta)}",
            $"Monto transferido: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}. Cuenta destino terminada en: {UltimosDigitos(destino.NumeroCuenta)}.");
        correoOk &= await EnviarCorreoSeguroAsync(receptor?.Email,
            $"Transacción enviada desde la cuenta {UltimosDigitos(origen.NumeroCuenta)}",
            $"Monto recibido: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}.");

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Transacción realizada correctamente." : MensajeCorreoFallido
        };
    }

    #endregion

    #region Pago a tarjeta de crédito

    public async Task<PagoTarjetaFormViewModel> BuildPagoTarjetaFormAsync(int clienteId)
    {
        var tarjetas = await _tarjetaRepo.GetByClienteIdAsync(clienteId);
        var tarjetasActivas = tarjetas.Where(t => t.Estado == "Activa")
            .Select(t => new TarjetaSelectItemViewModel { Id = t.Id, UltimosDigitos = UltimosDigitos(t.NumeroTarjeta), MontoAdeudado = t.MontoAdeudado })
            .ToList();

        return new PagoTarjetaFormViewModel
        {
            Tarjetas = tarjetasActivas,
            CuentasOrigen = await GetCuentasActivasSelectAsync(clienteId)
        };
    }

    private async Task<(string? error, CuentaAhorro? origen, TarjetaCredito? tarjeta, decimal montoEfectivo)> ValidarPagoTarjetaAsync(int clienteId, int cuentaOrigenId, int tarjetaId, decimal monto)
    {
        var origen = await _cuentaRepo.GetByIdAsync(cuentaOrigenId);
        if (origen == null || origen.ClienteId != clienteId || origen.Estado != "Activa")
            return ("La cuenta de origen seleccionada no es válida.", null, null, 0m);

        var tarjeta = await _tarjetaRepo.GetByIdAsync(tarjetaId);
        if (tarjeta == null || tarjeta.ClienteId != clienteId || tarjeta.Estado != "Activa")
            return ("La tarjeta seleccionada no es válida.", null, null, 0m);

        if (tarjeta.MontoAdeudado <= 0)
            return ("La tarjeta seleccionada no tiene deuda pendiente.", null, null, 0m);

        decimal montoEfectivo = Math.Min(monto, tarjeta.MontoAdeudado);

        if (origen.Balance < montoEfectivo)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, UltimosDigitos(tarjeta.NumeroTarjeta), clienteId);
            return ("No dispone del monto requerido en la cuenta seleccionada.", null, null, 0m);
        }

        return (null, origen, tarjeta, montoEfectivo);
    }

    public async Task<(string? error, PagoTarjetaConfirmViewModel? confirm)> PreviewPagoTarjetaAsync(int clienteId, PagoTarjetaFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, tarjeta, montoEfectivo) = await ValidarPagoTarjetaAsync(clienteId, model.CuentaOrigenId ?? 0, model.TarjetaId ?? 0, monto);
        if (error != null) return (error, null);

        var titular = await _usuarioRepo.GetByIdAsync(clienteId);
        var confirm = new PagoTarjetaConfirmViewModel
        {
            TarjetaId = tarjeta!.Id,
            CuentaOrigenId = origen!.Id,
            MontoIngresado = monto,
            MontoEfectivo = montoEfectivo,
            TitularCuentaOrigen = titular != null ? NombreCompleto(titular) : "",
            NumeroCuentaOrigen = origen.NumeroCuenta,
            TitularTarjeta = titular != null ? NombreCompleto(titular) : "",
            UltimosDigitosTarjeta = UltimosDigitos(tarjeta.NumeroTarjeta)
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarPagoTarjetaAsync(int clienteId, PagoTarjetaConfirmViewModel model)
    {
        var (error, origen, tarjeta, montoEfectivo) = await ValidarPagoTarjetaAsync(clienteId, model.CuentaOrigenId, model.TarjetaId, model.MontoIngresado);
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
            UsuarioResponsableId = clienteId,
            FechaTransaccion = fecha
        });

        var cliente = await _usuarioRepo.GetByIdAsync(clienteId);
        bool correoOk = await EnviarCorreoSeguroAsync(cliente?.Email,
            $"Pago realizado a la tarjeta {UltimosDigitos(tarjeta.NumeroTarjeta)}",
            $"Monto pagado: {FormatoMonto(montoEfectivo)}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Tarjeta terminada en: {UltimosDigitos(tarjeta.NumeroTarjeta)}. Fecha y hora: {fecha}.");

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Pago realizado correctamente." : MensajeCorreoFallido
        };
    }

    #endregion

    #region Pago a préstamo

    public async Task<PagoPrestamoFormViewModel> BuildPagoPrestamoFormAsync(int clienteId)
    {
        var prestamos = await _prestamoRepo.GetByClienteIdAsync(clienteId);
        var prestamosActivos = new List<PrestamoSelectItemViewModel>();
        foreach (var p in prestamos.Where(p => p.Estado == "Activo"))
        {
            decimal pendienteReal = await CalcularMontoPendienteRealAsync(p.Id);
            prestamosActivos.Add(new PrestamoSelectItemViewModel { Id = p.Id, NumeroPrestamo = p.NumeroPrestamo, MontoPendiente = pendienteReal });
        }

        return new PagoPrestamoFormViewModel
        {
            Prestamos = prestamosActivos,
            CuentasOrigen = await GetCuentasActivasSelectAsync(clienteId)
        };
    }

    private async Task<decimal> CalcularMontoPendienteRealAsync(int prestamoId)
    {
        var cuotas = await _cuotaRepo.GetByPrestamoIdAsync(prestamoId);
        return cuotas.Where(c => c.EstadoPago != "Pagada").Sum(c => c.SaldoPendiente);
    }

    private async Task<(string? error, CuentaAhorro? origen, Prestamo? prestamo, List<CuotaPrestamo>? cuotasPendientes, decimal montoEfectivo)> ValidarPagoPrestamoAsync(int clienteId, int cuentaOrigenId, int prestamoId, decimal monto)
    {
        var origen = await _cuentaRepo.GetByIdAsync(cuentaOrigenId);
        if (origen == null || origen.ClienteId != clienteId || origen.Estado != "Activa")
            return ("La cuenta de origen seleccionada no es válida.", null, null, null, 0m);

        var prestamo = await _prestamoRepo.GetByIdWithIncludesAsync(prestamoId);
        if (prestamo == null || prestamo.ClienteId != clienteId || prestamo.Estado != "Activo")
            return ("El préstamo seleccionado no es válido.", null, null, null, 0m);

        var cuotas = await _cuotaRepo.GetByPrestamoIdAsync(prestamo.Id);
        var cuotasPendientes = cuotas.Where(c => c.EstadoPago != "Pagada").OrderBy(c => c.NumeroCuota).ToList();

        if (!cuotasPendientes.Any())
            return ("El préstamo seleccionado no tiene cuotas pendientes de pago.", null, null, null, 0m);

        decimal montoPendienteReal = cuotasPendientes.Sum(c => c.SaldoPendiente);
        decimal montoEfectivo = Math.Min(monto, montoPendienteReal);

        if (origen.Balance < montoEfectivo)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, prestamo.NumeroPrestamo, clienteId);
            return ("No dispone del monto requerido en la cuenta seleccionada.", null, null, null, 0m);
        }

        return (null, origen, prestamo, cuotasPendientes, montoEfectivo);
    }

    public async Task<(string? error, PagoPrestamoConfirmViewModel? confirm)> PreviewPagoPrestamoAsync(int clienteId, PagoPrestamoFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, prestamo, _, montoEfectivo) = await ValidarPagoPrestamoAsync(clienteId, model.CuentaOrigenId ?? 0, model.PrestamoId ?? 0, monto);
        if (error != null) return (error, null);

        var titular = await _usuarioRepo.GetByIdAsync(clienteId);
        var confirm = new PagoPrestamoConfirmViewModel
        {
            PrestamoId = prestamo!.Id,
            CuentaOrigenId = origen!.Id,
            MontoIngresado = monto,
            MontoEfectivo = montoEfectivo,
            TitularCuentaOrigen = titular != null ? NombreCompleto(titular) : "",
            NumeroCuentaOrigen = origen.NumeroCuenta,
            TitularPrestamo = titular != null ? NombreCompleto(titular) : "",
            NumeroPrestamo = prestamo.NumeroPrestamo
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarPagoPrestamoAsync(int clienteId, PagoPrestamoConfirmViewModel model)
    {
        var (error, origen, prestamo, cuotasPendientes, montoEfectivo) = await ValidarPagoPrestamoAsync(clienteId, model.CuentaOrigenId, model.PrestamoId, model.MontoIngresado);
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
            UsuarioResponsableId = clienteId,
            FechaTransaccion = fecha
        });

        var cliente = await _usuarioRepo.GetByIdAsync(clienteId);
        bool correoOk = await EnviarCorreoSeguroAsync(cliente?.Email,
            $"Pago realizado al préstamo {prestamo.NumeroPrestamo}",
            $"Monto pagado: {FormatoMonto(montoEfectivo)}. Número de préstamo: {prestamo.NumeroPrestamo}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}. Fecha y hora: {fecha}.");

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Pago realizado correctamente." : MensajeCorreoFallido
        };
    }

    #endregion

    #region Transacción a beneficiarios

    public async Task<PagoBeneficiarioFormViewModel> BuildPagoBeneficiarioFormAsync(int clienteId)
    {
        var beneficiarios = await _beneficiarioRepo.GetAllByClienteIdAsync(clienteId);
        var beneficiariosVm = beneficiarios
            .Select(b => new BeneficiarioSelectItemViewModel { Id = b.Id, NombreCompleto = $"{b.Nombre} {b.Apellido}", NumeroCuenta = b.CuentaAhorro!.NumeroCuenta })
            .ToList();

        return new PagoBeneficiarioFormViewModel
        {
            Beneficiarios = beneficiariosVm,
            CuentasOrigen = await GetCuentasActivasSelectAsync(clienteId)
        };
    }

    private async Task<(string? error, CuentaAhorro? origen, Beneficiario? beneficiario, CuentaAhorro? cuentaBeneficiario)> ValidarPagoBeneficiarioAsync(int clienteId, int cuentaOrigenId, int beneficiarioId, decimal monto)
    {
        var beneficiarios = await _beneficiarioRepo.GetAllByClienteIdAsync(clienteId);
        if (!beneficiarios.Any())
            return ("No tiene beneficiarios registrados.", null, null, null);

        var beneficiario = beneficiarios.FirstOrDefault(b => b.Id == beneficiarioId);
        if (beneficiario == null)
            return ("El beneficiario seleccionado no es válido.", null, null, null);

        var cuentaBeneficiario = beneficiario.CuentaAhorro ?? await _cuentaRepo.GetByIdAsync(beneficiario.CuentaAhorroId);
        if (cuentaBeneficiario == null || cuentaBeneficiario.Estado != "Activa")
            return ("La cuenta del beneficiario no se encuentra disponible.", null, null, null);

        var origen = await _cuentaRepo.GetByIdAsync(cuentaOrigenId);
        if (origen == null || origen.ClienteId != clienteId || origen.Estado != "Activa")
            return ("La cuenta de origen seleccionada no es válida.", null, null, null);

        if (origen.Balance < monto)
        {
            await RegistrarTransaccionRechazadaAsync(origen.Id, origen.NumeroCuenta, monto, cuentaBeneficiario.NumeroCuenta, clienteId);
            return ("No dispone de fondos suficientes para realizar esta transacción.", null, null, null);
        }

        return (null, origen, beneficiario, cuentaBeneficiario);
    }

    public async Task<(string? error, PagoBeneficiarioConfirmViewModel? confirm)> PreviewPagoBeneficiarioAsync(int clienteId, PagoBeneficiarioFormViewModel model)
    {
        decimal monto = model.Monto ?? 0m;
        var (error, origen, beneficiario, cuentaBeneficiario) = await ValidarPagoBeneficiarioAsync(clienteId, model.CuentaOrigenId ?? 0, model.BeneficiarioId ?? 0, monto);
        if (error != null) return (error, null);

        var confirm = new PagoBeneficiarioConfirmViewModel
        {
            BeneficiarioId = beneficiario!.Id,
            CuentaOrigenId = origen!.Id,
            NumeroCuentaOrigen = origen.NumeroCuenta,
            Monto = monto,
            TitularBeneficiario = $"{beneficiario.Nombre} {beneficiario.Apellido}",
            NumeroCuentaBeneficiario = cuentaBeneficiario!.NumeroCuenta
        };
        return (null, confirm);
    }

    public async Task<OperationResultViewModel> EjecutarPagoBeneficiarioAsync(int clienteId, PagoBeneficiarioConfirmViewModel model)
    {
        var (error, origen, beneficiario, cuentaBeneficiario) = await ValidarPagoBeneficiarioAsync(clienteId, model.CuentaOrigenId, model.BeneficiarioId, model.Monto);
        if (error != null) return new OperationResultViewModel { Success = false, Message = error };

        origen!.Balance -= model.Monto;
        await _cuentaRepo.UpdateAsync(origen, origen.Id);

        cuentaBeneficiario!.Balance += model.Monto;
        await _cuentaRepo.UpdateAsync(cuentaBeneficiario, cuentaBeneficiario.Id);

        var fecha = DateTime.UtcNow;

        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaOrigenId = origen.Id,
            Monto = model.Monto,
            TipoTransaccion = "DÉBITO",
            Origen = origen.NumeroCuenta,
            Beneficiario = cuentaBeneficiario.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = clienteId,
            FechaTransaccion = fecha
        });

        await _transaccionRepo.AddAsync(new Transaccion
        {
            CuentaDestinoId = cuentaBeneficiario.Id,
            Monto = model.Monto,
            TipoTransaccion = "CRÉDITO",
            Origen = origen.NumeroCuenta,
            Beneficiario = cuentaBeneficiario.NumeroCuenta,
            Estado = "APROBADA",
            UsuarioResponsableId = clienteId,
            FechaTransaccion = fecha
        });

        var remitente = await _usuarioRepo.GetByIdAsync(clienteId);
        var receptor = await _usuarioRepo.GetByIdAsync(cuentaBeneficiario.ClienteId);

        bool correoOk = true;
        correoOk &= await EnviarCorreoSeguroAsync(remitente?.Email,
            $"Transacción realizada a la cuenta {UltimosDigitos(cuentaBeneficiario.NumeroCuenta)}",
            $"Monto transferido: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}. Cuenta destino terminada en: {UltimosDigitos(cuentaBeneficiario.NumeroCuenta)}.");
        correoOk &= await EnviarCorreoSeguroAsync(receptor?.Email,
            $"Transacción enviada desde la cuenta {UltimosDigitos(origen.NumeroCuenta)}",
            $"Monto recibido: {FormatoMonto(model.Monto)}. Fecha y hora: {fecha}. Cuenta origen terminada en: {UltimosDigitos(origen.NumeroCuenta)}.");

        return new OperationResultViewModel
        {
            Success = true,
            CorreoFallido = !correoOk,
            Message = correoOk ? "Transacción realizada correctamente." : MensajeCorreoFallido
        };
    }

    #endregion

    private async Task<List<CuentaSelectItemViewModel>> GetCuentasActivasSelectAsync(int clienteId)
    {
        var cuentas = await _cuentaRepo.GetByClienteIdAsync(clienteId);
        return cuentas.Where(c => c.Estado == "Activa")
            .Select(c => new CuentaSelectItemViewModel { Id = c.Id, NumeroCuenta = c.NumeroCuenta, Balance = c.Balance })
            .ToList();
    }
}
