using ArtemisBankingPro.Application.Exceptions;
using ArtemisBankingPro.Application.Interfaces.Repositories;
using ArtemisBankingPro.Application.Interfaces.Services;
using ArtemisBankingPro.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Application.Features.HermesPay.Commands.ProcessPayment;

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, Unit>
{
    private readonly ITarjetaCreditoRepository _tarjetaRepository;
    private readonly IComercioRepository _comercioRepository;
    private readonly ICuentaAhorroRepository _cuentaRepository;
    private readonly IConsumoTarjetaRepository _consumoRepository;
    private readonly ITransaccionRepository _transaccionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        ITarjetaCreditoRepository tarjetaRepository,
        IComercioRepository comercioRepository,
        ICuentaAhorroRepository cuentaRepository,
        IConsumoTarjetaRepository consumoRepository,
        ITransaccionRepository transaccionRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<ProcessPaymentCommandHandler> logger)
    {
        _tarjetaRepository = tarjetaRepository;
        _comercioRepository = comercioRepository;
        _cuentaRepository = cuentaRepository;
        _consumoRepository = consumoRepository;
        _transaccionRepository = transaccionRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Unit> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var comercio = await _comercioRepository.GetByIdWithIncludesAsync(request.CommerceId);
        if (comercio == null)
            throw new Exception("404");

        if (!comercio.EsActivo)
            throw new Exception("400:Un comercio inactivo no puede consultar ni procesar pagos.");

        if (comercio.ComercioUsuarioRel == null)
            throw new Exception("400:El comercio debe tener un usuario asociado.");

        var tarjeta = await _tarjetaRepository.GetByNumeroTarjetaAsync(request.CardNumber);
        if (tarjeta == null || tarjeta.Estado != "Activa")
            throw new Exception("400:La tarjeta no existe o está inactiva.");

        var requestHash = ComputeSha256Hash(request.Cvc);
        if (tarjeta.CVC != requestHash)
            throw new Exception("400:El CVC es incorrecto.");

        var expiracionParts = tarjeta.FechaExpiracion.Split('/');
        var mesExpiracionStr = expiracionParts[0];
        var anoExpiracionStr = expiracionParts[1];
        var anoExpiracion = 2000 + int.Parse(anoExpiracionStr);
        var mesExpiracion = int.Parse(mesExpiracionStr);

        var requestAnoExpiracion = request.YearExpirationCard.Length == 2 ? 2000 + int.Parse(request.YearExpirationCard) : int.Parse(request.YearExpirationCard);
        var requestMesExpiracion = int.Parse(request.MonthExpirationCard);

        if (anoExpiracion != requestAnoExpiracion || mesExpiracion != requestMesExpiracion)
            throw new Exception("400:La fecha de expiración no coincide.");

        var ultimoDiaMes = DateTime.DaysInMonth(anoExpiracion, mesExpiracion);
        var fechaVencimiento = new DateTime(anoExpiracion, mesExpiracion, ultimoDiaMes, 23, 59, 59);

        if (DateTime.UtcNow > fechaVencimiento)
            throw new Exception("400:La tarjeta está vencida.");

        var creditoDisponible = tarjeta.LimiteCredito - tarjeta.MontoAdeudado;
        if (request.TransactionAmount > creditoDisponible)
        {
            var consumoRechazado = new ConsumoTarjeta
            {
                TarjetaId = tarjeta.Id,
                Monto = request.TransactionAmount,
                Comercio = comercio.Nombre,
                Estado = "RECHAZADO",
                FechaConsumo = DateTime.UtcNow
            };
            await _consumoRepository.AddAsync(consumoRechazado);
            throw new Exception("400:El monto de la transacción excede el crédito disponible de la tarjeta.");
        }

        var cuentaComercio = await _cuentaRepository.GetCuentaPrincipalByUsuarioIdAsync(comercio.ComercioUsuarioRel.UsuarioId);
        if (cuentaComercio == null || cuentaComercio.Estado != "Activa")
            throw new Exception("400:El usuario de comercio debe tener una cuenta de ahorro principal activa.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            tarjeta.MontoAdeudado += request.TransactionAmount;
            await _tarjetaRepository.UpdateAsync(tarjeta, tarjeta.Id);

            var consumo = new ConsumoTarjeta
            {
                TarjetaId = tarjeta.Id,
                Monto = request.TransactionAmount,
                Comercio = comercio.Nombre,
                Estado = "APROBADO",
                FechaConsumo = DateTime.UtcNow
            };
            await _consumoRepository.AddAsync(consumo);

            cuentaComercio.Balance += request.TransactionAmount;
            await _cuentaRepository.UpdateAsync(cuentaComercio, cuentaComercio.Id);

            string ultimos4 = tarjeta.NumeroTarjeta.Substring(12);

            var transaccion = new Transaccion
            {
                CuentaDestinoId = cuentaComercio.Id,
                Monto = request.TransactionAmount,
                TipoTransaccion = "CRÉDITO",
                Origen = ultimos4,
                Beneficiario = cuentaComercio.NumeroCuenta,
                Estado = "APROBADA",
                FechaTransaccion = DateTime.UtcNow,
                UsuarioResponsableId = comercio.ComercioUsuarioRel.UsuarioId
            };
            await _transaccionRepository.AddAsync(transaccion);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Pago procesado mediante Hermes Pay. Comercio: {Comercio}, Monto: {Monto}, Tarjeta: ****{Ultimos4}", comercio.Nombre, request.TransactionAmount, ultimos4);

            try
            {
                if (tarjeta.Cliente != null)
                {
                    await _emailService.SendEmailAsync(tarjeta.Cliente.Email!, $"Consumo realizado con la tarjeta {ultimos4}", 
                        $"Hola {tarjeta.Cliente.Nombre},\n\nSe ha realizado un consumo con su tarjeta terminada en {ultimos4}.\nComercio: {comercio.Nombre}\nMonto: RD${request.TransactionAmount}\nFecha y hora: {DateTime.UtcNow}\n\nSi usted no reconoce esta operación, comuníquese con la entidad bancaria.");
                }

                await _emailService.SendEmailAsync(comercio.Correo, $"Pago recibido a través de tarjeta {ultimos4}", 
                    $"Hola {comercio.Nombre},\n\nHa recibido un nuevo pago mediante Hermes Pay.\nTarjeta terminada en: {ultimos4}\nMonto recibido: RD${request.TransactionAmount}\nFecha y hora: {DateTime.UtcNow}\n\nEste mensaje sirve como constancia del pago recibido.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al enviar correo de notificación en Hermes Pay");
            }
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Error al procesar el pago transaccionalmente");
            throw new Exception("500:Error interno al procesar el pago.");
        }

        return Unit.Value;
    }

    private string ComputeSha256Hash(string rawData)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
