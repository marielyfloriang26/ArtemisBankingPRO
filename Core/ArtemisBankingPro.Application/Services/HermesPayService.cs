using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ArtemisBankingPro.Application.DTOs;
using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;

namespace ArtemisBankingPro.Application.Services;

public class HermesPayService : IHermesPayService
{
    private readonly IGenericRepository<TarjetaCredito> _tarjetaRepository;
    private readonly IGenericRepository<CuentaAhorro> _cuentaRepository;
    private readonly IGenericRepository<ConsumoTarjeta> _consumoRepository;
    private readonly IGenericRepository<Transaccion> _transaccionRepository;
    private readonly IGenericRepository<Comercio> _comercioRepository;
    private readonly IEmailService _emailService;
    private readonly UserManager<Usuario> _userManager;
    private readonly ILogger<HermesPayService> _logger;

    public HermesPayService(
        IGenericRepository<TarjetaCredito> tarjetaRepository,
        IGenericRepository<CuentaAhorro> cuentaRepository,
        IGenericRepository<ConsumoTarjeta> consumoRepository,
        IGenericRepository<Transaccion> transaccionRepository,
        IGenericRepository<Comercio> comercioRepository,
        IEmailService emailService,
        UserManager<Usuario> userManager,
        ILogger<HermesPayService> logger)
    {
        _tarjetaRepository = tarjetaRepository;
        _cuentaRepository = cuentaRepository;
        _consumoRepository = consumoRepository;
        _transaccionRepository = transaccionRepository;
        _comercioRepository = comercioRepository;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, int StatusCode, object? Data)> GetCommerceTransactionsAsync(
        int routeCommerceId, string userId, string userRole, int page, int pageSize)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > 20)
            return (false, "Los parámetros de paginación son inválidos. pageSize máximo es 20.", 400, null);

        var (targetCommerce, errorMsg, statusCode) = await ResolvingCommerceAsync(routeCommerceId, userId, userRole);
        if (targetCommerce == null)
            return (false, errorMsg, statusCode, null);

        int usuarioAsociadoId = targetCommerce.ComercioUsuarioRel?.UsuarioId ?? 0;

        var transacciones = await _transaccionRepository.GetAllAsync();
        var comercioCuenta = (await _cuentaRepository.GetAllAsync())
            .FirstOrDefault(c => c.ClienteId == usuarioAsociadoId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (comercioCuenta == null)
            return (false, "El comercio no posee una cuenta de ahorro principal activa.", 400, null);

        var listQuery = transacciones
            .Where(t => t.CuentaDestinoId == comercioCuenta.Id)
            .OrderByDescending(t => t.FechaTransaccion);

        int totalRecords = listQuery.Count();
        int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var data = listQuery.Skip((page - 1) * pageSize).Take(pageSize).Select(t => new {
            id = t.Id.ToString(),
            transactionDate = t.FechaTransaccion.ToString("yyyy-MM-ddTHH:mm:ss"),
            amount = t.Monto,
            cardLastFourDigits = t.Origen.Length >= 4 ? t.Origen.Substring(t.Origen.Length - 4) : "****",
            status = t.Estado
        }).ToList();

        var response = new {
            page = page,
            pageSize = pageSize,
            totalRecords = totalRecords,
            totalPages = totalPages,
            commerceId = targetCommerce.Id,
            commerceName = targetCommerce.Nombre,
            data = data
        };

        return (true, "OK", 200, response);
    }

    public async Task<(bool Success, string Message, int StatusCode)> ProcessPaymentAsync(
        int routeCommerceId, string userId, string userRole, ProcessPaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber) || request.CardNumber.Length != 16)
            return (false, "El número de tarjeta debe contener exactamente 16 dígitos.", 400);

        if (string.IsNullOrWhiteSpace(request.MonthExpirationCard) || string.IsNullOrWhiteSpace(request.YearExpirationCard))
            return (false, "La fecha de expiración es requerida.", 400);

        if (string.IsNullOrWhiteSpace(request.Cvc) || request.Cvc.Length != 3)
            return (false, "El CVC debe contener exactamente 3 dígitos.", 400);

        if (request.TransactionAmount <= 0)
            return (false, "El monto de la transacción debe ser mayor que cero.", 400);

        var (comercio, errorMsg, statusCode) = await ResolvingCommerceAsync(routeCommerceId, userId, userRole);
        if (comercio == null)
            return (false, errorMsg, statusCode);

        int usuarioAsociadoId = comercio.ComercioUsuarioRel?.UsuarioId ?? 0;

        var cuentaComercio = (await _cuentaRepository.GetAllAsync())
            .FirstOrDefault(c => c.ClienteId == usuarioAsociadoId && c.TipoCuenta == "Principal" && c.Estado == "Activa");

        if (cuentaComercio == null)
            return (false, "El comercio no tiene una cuenta de ahorro principal activa.", 400);

        var tarjetas = await _tarjetaRepository.GetAllAsync();
        var tarjeta = tarjetas.FirstOrDefault(t => t.NumeroTarjeta == request.CardNumber);

        if (tarjeta == null || tarjeta.Estado != "Activa")
            return (false, "La tarjeta no existe o se encuentra inactiva.", 400);

        string expInput = $"{request.MonthExpirationCard.PadLeft(2, '0')}/{request.YearExpirationCard.Substring(request.YearExpirationCard.Length - 2)}";
        if (tarjeta.FechaExpiracion != expInput)
            return (false, "La fecha de expiración proporcionada no coincide.", 400);

        if (tarjeta.CVC != HashSHA256(request.Cvc))
            return (false, "Los datos de la tarjeta son incorrectos.", 400);

        decimal creditoDisponible = tarjeta.LimiteCredito - tarjeta.MontoAdeudado;
        DateTime ahora = DateTime.UtcNow;

        if (request.TransactionAmount > creditoDisponible)
        {
            var consumoRechazado = new ConsumoTarjeta
            {
                TarjetaId = tarjeta.Id,
                Monto = request.TransactionAmount,
                Comercio = comercio.Nombre,
                Estado = "RECHAZADO",
                FechaConsumo = ahora
            };
            await _consumoRepository.AddAsync(consumoRechazado);

            return (false, "El monto de la transacción excede el crédito disponible de la tarjeta.", 400);
        }

        tarjeta.MontoAdeudado += request.TransactionAmount;
        cuentaComercio.Balance += request.TransactionAmount;

        var consumoAprobado = new ConsumoTarjeta
        {
            TarjetaId = tarjeta.Id,
            Monto = request.TransactionAmount,
            Comercio = comercio.Nombre,
            Estado = "APROBADO",
            FechaConsumo = ahora
        };

        string ultimosCuatro = tarjeta.NumeroTarjeta.Substring(12);

        var transaccionComercio = new Transaccion
        {
            CuentaDestinoId = cuentaComercio.Id,
            Monto = request.TransactionAmount,
            TipoTransaccion = "CRÉDITO",
            Origen = ultimosCuatro,
            Beneficiario = cuentaComercio.NumeroCuenta,
            Estado = "APROBADA",
            FechaTransaccion = ahora
        };

        await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);
        await _cuentaRepository.UpdateAsync(cuentaComercio, cuentaComercio.Id);
        await _consumoRepository.AddAsync(consumoAprobado);
        await _transaccionRepository.AddAsync(transaccionComercio);

        _ = Task.Run(async () => {
            try
            {
                var clienteTarjeta = await _userManager.FindByIdAsync(tarjeta.ClienteId.ToString());
                if (clienteTarjeta != null && !string.IsNullOrEmpty(clienteTarjeta.Email))
                {
                    string asuntoCliente = $"Consumo realizado con la tarjeta {ultimosCuatro}";
                    string cuerpoCliente = $"Hola {clienteTarjeta.Nombre},\n\nSe ha realizado un consumo con su tarjeta terminada en {ultimosCuatro}.\n\nComercio: {comercio.Nombre}\nMonto: RD${request.TransactionAmount:N2}\nFecha y hora: {ahora:yyyy-MM-dd HH:mm:ss}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria.";
                    await _emailService.SendEmailAsync(clienteTarjeta.Email, asuntoCliente, cuerpoCliente);
                }

                if (!string.IsNullOrEmpty(comercio.Correo))
                {
                    string asuntoComercio = $"Pago recibido a través de tarjeta {ultimosCuatro}";
                    string cuerpoComercio = $"Hola {comercio.Nombre},\n\nHa recibido un nuevo pago mediante Hermes Pay.\n\nTarjeta terminada en: {ultimosCuatro}\nMonto recibido: RD${request.TransactionAmount:N2}\nFecha y hora: {ahora:yyyy-MM-dd HH:mm:ss}\n\nEste mensaje sirve como constancia del pago recibido.";
                    await _emailService.SendEmailAsync(comercio.Correo, asuntoComercio, cuerpoComercio);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar las notificaciones de correo para Hermes Pay.");
            }
        });

        return (true, "Pago procesado correctamente.", 204);
    }

    private async Task<(Comercio? comercio, string error, int statusCode)> ResolvingCommerceAsync(int routeCommerceId, string userId, string userRole)
    {
        var comercios = await _comercioRepository.GetAllAsync();
        Comercio? comercio = null;

        if (userRole == "Comercio")
        {
            if (!int.TryParse(userId, out int parsedUserId))
                return (null, "Acceso denegado.", 403);

            comercio = comercios.FirstOrDefault(c => c.ComercioUsuarioRel != null && c.ComercioUsuarioRel.UsuarioId == parsedUserId);
            if (comercio == null)
                return (null, "El usuario de comercio no tiene un comercio asociado.", 403);
        }
        else if (userRole == "Administrador")
        {
            comercio = comercios.FirstOrDefault(c => c.Id == routeCommerceId);
            if (comercio == null)
                return (null, "El comercio indicado no existe.", 404);
        }

        if (comercio != null && !comercio.EsActivo)
            return (null, "El comercio se encuentra inactivo.", 400);

        return (comercio, string.Empty, 200);
    }

    private string HashSHA256(string rawData)
    {
        using var sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }
}